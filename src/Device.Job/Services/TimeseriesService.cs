using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Job.Model;
using Device.Job.Constant;
using Device.Domain.Entity;
using Device.Application.Constant;
using Device.Application.Service;
using Device.Application.Repository;
using Device.Job.Service.Abstraction;
using Device.Application.Asset.Command;
using Device.Application.Historical.Query;
using Device.Application.Historical.Query.Model;
using Device.Application.Service.Abstraction;
using Device.Application.Asset.Command.Model;
using Newtonsoft.Json;
using Device.Application.Extension;
using AHI.Infrastructure.MultiTenancy.Extension;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Newtonsoft.Json.Linq;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Microsoft.Extensions.Configuration;

namespace Device.Job.Service
{
    public class TimeseriesService : IDataSourceService
    {
        private readonly IAssetService _assetService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMediator _mediator;
        private readonly ILoggerAdapter<TimeseriesService> _logger;
        private readonly IConfiguration _configuration;

        public TimeseriesService(IAssetService assetService,
                IServiceProvider serviceProvider,
                IMediator mediator,
                ILoggerAdapter<TimeseriesService> logger,
                IConfiguration configuration)
        {
            _assetService = assetService;
            _serviceProvider = serviceProvider;
            _mediator = mediator;
            _logger = logger;
            _configuration = configuration;
        }

        public async IAsyncEnumerable<IEnumerable<FlattenHistoricalData>> GetDataAsync(ITenantContext tenantContext, JobInfo jobInfo)
        {
            ITenantContext requestTenantContext = null;
            var assetTimeseries = JsonConvert.DeserializeObject<AssetTimeseries>(JsonConvert.SerializeObject(jobInfo.Payload));
            var start = DateTimeOffset.FromUnixTimeMilliseconds(assetTimeseries.Start).DateTime;
            var end = DateTimeOffset.FromUnixTimeMilliseconds(assetTimeseries.End).DateTime;
            var periodInSeconds = GetPeriodInSeconds(assetTimeseries.TimeGrain);
            var ranges = GetDateTimeRanges(start, end, periodInSeconds);
            var attributes = GetAttributes(assetTimeseries.AttributeIds, assetTimeseries.ColumnNames);
            var assetDto = await _assetService.FindAssetByIdAsync(new GetAssetById(assetTimeseries.AssetId), CancellationToken.None);
            foreach (var range in ranges)
            {

                requestTenantContext = tenantContext.Clone();
                requestTenantContext.SetSubscriptionId(assetTimeseries.SubscriptionId);
                requestTenantContext.SetProjectId(assetTimeseries.ProjectId);

                using (var newScope = _serviceProvider.CreateNewScope(requestTenantContext))
                {
                    assetTimeseries.Start = new DateTimeOffset(range.Start).ToUnixTimeMilliseconds();
                    assetTimeseries.End = new DateTimeOffset(range.End).ToUnixTimeMilliseconds();
                    var historicalData = await GetTimeseriesDataAsync(newScope, assetDto, assetTimeseries);
                    yield return historicalData.SelectMany(hi => hi.Attributes.SelectMany(at => at.Series.Select(se => new FlattenHistoricalData(hi.AssetId.ToString(), at.AttributeId, GetAttributeName(attributes, at.AttributeId, at.AttributeNameNormalize), se.ts, se.v)))
                                                .OrderBy(x => x.UnixTimestamp));
                }

            }
        }

        public async IAsyncEnumerable<List<FlattenHistoricalData>> GetPaginationDataAsync(GetFullAssetAttributeSeries assetTimeseries, Guid activityId, Guid widgetId)
        {
            int timeseriesLimit = string.IsNullOrEmpty(_configuration["ApplicationSetings:TimeseriesLimit"].ToString()) ? Series.TIMESERIES_LIMIT : int.Parse(_configuration["ApplicationSetings:TimeseriesLimit"].ToString());

            var getPagingCommand = JObject.FromObject(assetTimeseries).ToObject<PaginationGetAssetAttributeSeries>();
            getPagingCommand.BePaging = true;
            getPagingCommand.PageSize = timeseriesLimit;
            getPagingCommand.PageIndex = 0;
            getPagingCommand.ActivityId = activityId;
            getPagingCommand.WidgetId = widgetId;

            while (true)
            {
                var start = DateTime.UtcNow;
                _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | ProcessQueryTimeSeriesFullDataAsync - Start GetPaginationDataAsync | pageIndex: {getPagingCommand.PageIndex}");

                var pagedData = await _mediator.Send(getPagingCommand);

                _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | ProcessQueryTimeSeriesFullDataAsync - End process getPagingCommand, total pagedData: {pagedData.Count} after {DateTime.UtcNow.Subtract(start).TotalMilliseconds} at {DateTime.UtcNow} | pageIndex: {getPagingCommand.PageIndex}");
                start = DateTime.UtcNow;

                var output = pagedData.SelectMany(hi => hi.Attributes.SelectMany(at => at.Series.Select(se =>
                {
                    if (se.v == null && string.IsNullOrEmpty(getPagingCommand.Assets.FirstOrDefault(x => x.AssetId == hi.AssetId)?.TimeGrain) && at.QualityCode == 0)
                        se.v = "Unknown";

                    return new FlattenHistoricalData(hi.AssetId.ToString(), at.AttributeId, string.Empty, se.ts, se.v);
                }))).ToList();

                if (!output.Any())
                {
                    break;
                }

                _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | ProcessQueryTimeSeriesFullDataAsync - End pagedData.SelectMany after {DateTime.UtcNow.Subtract(start).TotalMilliseconds} at {DateTime.UtcNow} | pageIndex: {getPagingCommand.PageIndex}");
                start = DateTime.UtcNow;

                yield return output;
                getPagingCommand.PageIndex++;

                _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | ProcessQueryTimeSeriesFullDataAsync - End GetPaginationDataAsync PageIndex++ after {DateTime.UtcNow.Subtract(start).TotalMilliseconds} at {DateTime.UtcNow} | pageIndex: {getPagingCommand.PageIndex}");
            }
        }

        private async Task<IEnumerable<HistoricalDataDto>> GetTimeseriesDataAsync(IServiceScope serviceScope, GetAssetDto assetDto, AssetAttributeSeries assetTimeseries, string timezoneOffset = Series.DEFAULT_TIMEZONE_OFFSET)
        {
            var timeSeriesRepository = serviceScope.ServiceProvider.GetRequiredService<IAssetTimeSeriesRepository>();
            var runtimeTimeSeriesRepository = serviceScope.ServiceProvider.GetRequiredService<IAssetRuntimeTimeSeriesRepository>();
            var referenceTimeSeries = serviceScope.ServiceProvider.GetRequiredService<IAssetAliasTimeSeriesRepository>();

            var request = new GetHistoricalData(assetTimeseries.Start, assetTimeseries.End, assetTimeseries.TimeGrain, assetTimeseries.Aggregate,
                                                        Series.DEFAULT_QUERY_TIMEOUT, assetTimeseries.AssetId, assetTimeseries.AttributeIds,
                                                        assetTimeseries.UseCustomTimeRange, assetTimeseries.RequestType, timezoneOffset,
                                                        assetTimeseries.GapfillFunction, assetTimeseries.PageSize);


            var attributes = assetDto.Attributes.Where(x => request.AttributeIds.Contains(x.Id)).Select(x => AssetAttributeDto.CreateHistoricalEntity(assetDto, x)).ToList();
            if (!attributes.Any())
            {
                return Array.Empty<HistoricalDataDto>();
            }
            var timeStart = DateTimeOffset.FromUnixTimeMilliseconds(request.TimeStart).DateTime;
            var timeEnd = DateTimeOffset.FromUnixTimeMilliseconds(request.TimeEnd).DateTime;

            // aggreagate the data from integration and internal
            var tasks = new List<Task<IEnumerable<TimeSeries>>>();

            // for internal -> call into database
            var snapshotAttributes = attributes.Where(x => AttributeTypeConstants.TYPE_DYNAMIC == x.AttributeType);
            if (snapshotAttributes.Any())
                tasks.Add(timeSeriesRepository.QueryDataAsync(request.TimezoneOffset, timeStart, timeEnd, request.TimeGrain, request.Aggregate, snapshotAttributes, request.TimeoutInSecond, request.GapfillFunction, request.PageSize, request.Quality));

            // for runtime internal -> call into database with runtime table
            var runtimeAttributes = attributes.Where(x => AttributeTypeConstants.TYPE_RUNTIME == x.AttributeType);
            if (runtimeAttributes.Any())
                tasks.Add(runtimeTimeSeriesRepository.QueryDataAsync(request.TimezoneOffset, timeStart, timeEnd, request.TimeGrain, request.Aggregate, runtimeAttributes, request.TimeoutInSecond, request.GapfillFunction, request.PageSize));

            // alias
            var aliasAttributes = attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS);
            if (aliasAttributes.Any())
                tasks.Add(referenceTimeSeries.QueryDataAsync(request.TimezoneOffset, timeStart, timeEnd, request.TimeGrain, request.Aggregate, aliasAttributes, request.TimeoutInSecond, request.GapfillFunction, request.PageSize));

            var taskResult = await Task.WhenAll(tasks);
            var finalResult = taskResult.SelectMany(x => x);

            var metrics = AssetSnapshotService.Join(attributes, finalResult, request, null);

            return metrics;
        }

        private string GetAttributeName(IDictionary<Guid, string> attributes, Guid attributeId, string attributeNameNormalize)
        {
            string attributeName = null;
            if (attributes.ContainsKey(attributeId))
                attributeName = attributes[attributeId];
            return string.IsNullOrEmpty(attributeName) ? attributeNameNormalize : attributeName;
        }

        private IDictionary<Guid, string> GetAttributes(IEnumerable<Guid> attributeIds, IEnumerable<string> columnNames)
        {
            return attributeIds.Distinct().Select((id, index) => new Job.Model.Attribute(id, GetColumnName(columnNames, index))).ToDictionary(x => x.Id, y => y.Name);
        }

        private string GetColumnName(IEnumerable<string> columnNames, int index)
        {
            try
            {
                return columnNames.ElementAt(index);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private int GetPeriodInSeconds(string timeGrain)
        {
            // timeGrain 1 minute, 5 minutes, 10 minutes,... will get data per 10 days (maxinum 14,400 data points)
            if (!string.IsNullOrEmpty(timeGrain))
            {
                return 10 * 24 * 3600;
            }

            // raw data will get data per 10 hours (maxinum 18,000 data points 2s/point)
            return 10 * 3600;
        }

        private IEnumerable<(DateTime Start, DateTime End)> GetDateTimeRanges(DateTime start, DateTime end, int periodInSeconds)
        {
            var ranges = new List<(DateTime, DateTime)>();
            var cursor = start;
            while (cursor < end)
            {
                if ((end - cursor).TotalSeconds < periodInSeconds)
                {
                    ranges.Add((cursor, end));
                    break;
                }

                ranges.Add((cursor, cursor.AddSeconds(periodInSeconds)));

                // +1 seconds into next start range to make it doesn't overlap previous end range
                cursor = cursor.AddSeconds(periodInSeconds + 1);
            }

            return ranges;
        }
    }
}