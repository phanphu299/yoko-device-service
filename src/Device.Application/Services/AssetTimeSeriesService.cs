using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using System.Linq.Dynamic.Core;
using Device.Application.Constant;
using Device.Application.Historical.Query.Model;
using Device.Application.Historical.Query;
using Device.Application.Repository;
using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Device.Domain.Entity;
using AHI.Infrastructure.Exception;
using Device.Application.Asset.Command.Model;
using AHI.Infrastructure.SharedKernel;

/*
 IMPORTANCE: High performance api should be reviewed and approved by technical team.
 PLEASE DO NOT CHANGE OR MODIFY THE LOGIC IF YOU DONT UNDERSTAND
 Author: Thanh Tran
 Email: thanh.tran@yokogawa.com
*/
namespace Device.Application.Service
{
    public class AssetTimeSeriesService : IAssetTimeSeriesService
    {
        private readonly IAssetTimeSeriesRepository _timeSeriesRepository;
        private readonly IAssetService _assetService;
        private readonly IAssetAliasTimeSeriesRepository _referenceTimeSeries;
        private readonly IAssetRuntimeTimeSeriesRepository _runtimeTimeSeriesRepository;
        private readonly ILoggerAdapter<AssetTimeSeriesService> _logger;
        private readonly IDeviceSignalQualityService _deviceSignalQualityService;
        private const int SIGNAL_QUALITY_CODE_GOOD = 192;
        public AssetTimeSeriesService(
                      IAssetService assetService
                    , IAssetTimeSeriesRepository timeSeriesRepository
                    , IAssetAliasTimeSeriesRepository referenceTimeSeries
                    , IAssetRuntimeTimeSeriesRepository runtimeTimeSeriesRepository
                    , ILoggerAdapter<AssetTimeSeriesService> logger,
                    IDeviceSignalQualityService deviceSignalQualityService)
        {
            _timeSeriesRepository = timeSeriesRepository;
            _runtimeTimeSeriesRepository = runtimeTimeSeriesRepository;
            _assetService = assetService;
            _referenceTimeSeries = referenceTimeSeries;
            _logger = logger;
            _deviceSignalQualityService = deviceSignalQualityService;
        }
        public async Task<IEnumerable<HistoricalDataDto>> GetTimeSeriesDataAsync(GetHistoricalData command, CancellationToken token)
        {
            try
            {
                var totalTime = DateTime.UtcNow;
                var start = DateTime.UtcNow;
                GetAssetDto assetDto;

                try
                {
                    assetDto = await _assetService.FindAssetByIdOptimizedAsync(new Asset.Command.GetAssetById(command.AssetId), token);
                }
                catch (EntityValidationException)
                {
                    return Array.Empty<HistoricalDataDto>();
                }

                var attributes = assetDto.Attributes.Where(x => command.AttributeIds.Contains(x.Id)).Select(x => Asset.Command.Model.AssetAttributeDto.CreateHistoricalEntity(assetDto, x)).ToList();
                _logger.LogDebug($"Query metadata take: {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
                start = DateTime.UtcNow;
                if (!attributes.Any())
                {
                    return Array.Empty<HistoricalDataDto>();
                }
                var timeStart = DateTimeOffset.FromUnixTimeMilliseconds(command.TimeStart).DateTime;
                var timeEnd = DateTimeOffset.FromUnixTimeMilliseconds(command.TimeEnd).DateTime;

                // aggreagate the data from integration and internal
                var tasks = new List<Task<IEnumerable<TimeSeries>>>();

                // for integration -> call into broker
                // consider to remove the integration, no need to use it any more
                // var integrationAttributes = attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_INTEGRATION);
                // if (integrationAttributes.Any())
                //     tasks.Add(_integrationTimeSeriesRepository.QueryDataAsync(request.TimezoneOffset, timeStart, timeEnd, request.TimeGrain, request.Aggregate, integrationAttributes, request.TimeoutInSecond, request.GapfillFunction));

                // for internal -> call into database
                var snapshotAttributes = attributes.Where(x => AttributeTypeConstants.TYPE_DYNAMIC == x.AttributeType);
                if (snapshotAttributes.Any())
                    tasks.Add(_timeSeriesRepository.QueryDataAsync(command.TimezoneOffset, timeStart, timeEnd, command.TimeGrain, command.Aggregate, snapshotAttributes, command.TimeoutInSecond, command.GapfillFunction, command.PageSize, command.Quality));

                // for runtime internal -> call into database with runtime table
                var runtimeAttributes = attributes.Where(x => AttributeTypeConstants.TYPE_RUNTIME == x.AttributeType);
                if (runtimeAttributes.Any())
                    tasks.Add(_runtimeTimeSeriesRepository.QueryDataAsync(command.TimezoneOffset, timeStart, timeEnd, command.TimeGrain, command.Aggregate, runtimeAttributes, command.TimeoutInSecond, command.GapfillFunction, command.PageSize));

                // alias
                var aliasAttributes = attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS);
                if (aliasAttributes.Any())
                    tasks.Add(_referenceTimeSeries.QueryDataAsync(command.TimezoneOffset, timeStart, timeEnd, command.TimeGrain, command.Aggregate, aliasAttributes, command.TimeoutInSecond, command.GapfillFunction, command.PageSize));

                // static
                var staticAttributes = attributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_STATIC);
                var staticMetrics = new List<TimeSeries>();
                if (staticAttributes.Any())
                {
                    bool isCount = command.Aggregate == TimeSeriesAggregateConstants.COUNT;
                    var (metricData, _) = GetStaticMetrics(isCount, timeStart, timeEnd, staticAttributes, assetDto.Attributes);
                    staticMetrics.AddRange(metricData);
                }

                var taskResult = await Task.WhenAll(tasks);
                var finalResult = taskResult.SelectMany(x => x).Union(staticMetrics);
                _logger.LogDebug($"Query timeseries data take: {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
                start = DateTime.UtcNow;

                var signalQualities = await _deviceSignalQualityService.GetAllSignalQualityAsync();
                var metrics = AssetSnapshotService.Join(attributes, finalResult, command, signalQualities);
                _logger.LogDebug($"Total query: {DateTime.UtcNow.Subtract(totalTime).TotalMilliseconds}");
                return metrics;
            }
            catch (EntityNotFoundException)
            {
                return Array.Empty<HistoricalDataDto>();
            }
        }

        public async Task<PaginationHistoricalDataDto> PaginationGetTimeSeriesDataAsync(PaginationGetHistoricalData command, CancellationToken token)
        {
            //TODO: this query is slow, need to improve it (take 1.5s)
            try
            {
                var totalTime = DateTime.UtcNow;
                var start = DateTime.UtcNow;
                GetAssetDto assetDto;

                try
                {
                    assetDto = await _assetService.FindAssetByIdAsync(new Asset.Command.GetAssetById(command.AssetId), token);
                }
                catch (EntityValidationException)
                {
                    return default;
                }

                var assetAttribute = assetDto.Attributes.Where(x => command.AttributeIds.Contains(x.Id)).FirstOrDefault();
                if (assetAttribute == null)
                {
                    return default;
                }

                var historicalEntity = AssetAttributeDto.CreateHistoricalEntity(assetDto, assetAttribute);
                _logger.LogInformation($"CorrelationId: {command.ActivityId} | widgetId: {command.WidgetId} | PaginationGetTimeSeriesDataAsync - Query metadata take: {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
                start = DateTime.UtcNow;

                var timeStart = DateTimeOffset.FromUnixTimeMilliseconds(command.TimeStart).DateTime;
                var timeEnd = DateTimeOffset.FromUnixTimeMilliseconds(command.TimeEnd).DateTime;

                (IEnumerable<TimeSeries> Series, int TotalCount) paginationData = (Array.Empty<TimeSeries>(), 0);

                // for internal -> call into database
                if (historicalEntity.AttributeType == AttributeTypeConstants.TYPE_DYNAMIC)
                    paginationData = await _timeSeriesRepository.PaginationQueryDataAsync(command.TimezoneOffset, timeStart, timeEnd, command.TimeGrain, command.Aggregate, historicalEntity, command.TimeoutInSecond, command.GapfillFunction, command.PageIndex, command.PageSize, command.Quality);

                // for runtime internal -> call into database with runtime table
                if (historicalEntity.AttributeType == AttributeTypeConstants.TYPE_RUNTIME)
                    paginationData = await _runtimeTimeSeriesRepository.PaginationQueryDataAsync(command.TimezoneOffset, timeStart, timeEnd, command.TimeGrain, command.Aggregate, historicalEntity, command.TimeoutInSecond, command.GapfillFunction, command.PageIndex, command.PageSize);

                // alias
                if (historicalEntity.AttributeType == AttributeTypeConstants.TYPE_ALIAS)
                    paginationData = await _referenceTimeSeries.PaginationQueryDataAsync(command.TimezoneOffset, timeStart, timeEnd, command.TimeGrain, command.Aggregate, historicalEntity, command.TimeoutInSecond, command.GapfillFunction, command.PageIndex, command.PageSize);

                // static
                bool isCount = command.Aggregate == TimeSeriesAggregateConstants.COUNT;
                if (historicalEntity.AttributeType == AttributeTypeConstants.TYPE_STATIC)
                    paginationData = GetStaticMetrics(isCount, timeStart, timeEnd, new List<HistoricalEntity> { historicalEntity }, new List<AssetAttributeDto> { assetAttribute });

                if (paginationData.Series == null || !paginationData.Series.Any())
                {
                    return default;
                }

                _logger.LogInformation($"CorrelationId: {command.ActivityId} | widgetId: {command.WidgetId} | PaginationGetTimeSeriesDataAsync - Query timeseries data take: {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");

                var signalQualities = await _deviceSignalQualityService.GetAllSignalQualityAsync();
                var metrics = AssetSnapshotService.Join(new List<HistoricalEntity> { historicalEntity }, paginationData.Series, command, signalQualities);
                _logger.LogInformation($"CorrelationId: {command.ActivityId} | widgetId: {command.WidgetId} | PaginationGetTimeSeriesDataAsync - Total query: {DateTime.UtcNow.Subtract(totalTime).TotalMilliseconds}");

                PaginationHistoricalDataDto historicalData = PaginationHistoricalDataDto.Create(metrics.First());
                historicalData.Attributes = historicalData.Attributes.Select(x =>
                {
                    x.PageIndex = command.PageIndex;
                    x.PageSize = command.PageSize;
                    x.TotalCount = paginationData.TotalCount;
                    return x;
                }).ToList();

                return historicalData;
            }
            catch (EntityNotFoundException)
            {
                return default;
            }
        }

        private (IEnumerable<TimeSeries>, int) GetStaticMetrics(
            bool isCount,
            DateTime start,
            DateTime end,
            IEnumerable<HistoricalEntity> historicalEntities,
            IEnumerable<AssetAttributeDto> staticAttributes)
        {
            var data = historicalEntities.Select(s =>
            {
                var attr = staticAttributes.FirstOrDefault(x => x.Id == s.AttributeId);
                if (attr == null || attr.UpdatedUtc < start || attr.UpdatedUtc > end)
                    return null;

                var value = attr.Payload != null ? attr.Payload.Value : attr.Value ?? string.Empty;
                var valueDouble = (double?)null;
                if (s.DataType == DataTypeConstants.TYPE_DOUBLE || s.DataType == DataTypeConstants.TYPE_INTEGER)
                {
                    valueDouble = double.TryParse(value.ToString(), out var val) ? val : (double?)null;
                }

                return new TimeSeries
                {
                    AttributeId = s.AttributeId,
                    DataType = s.DataType,
                    AssetId = s.AssetId,
                    ValueText = isCount ? "1" : value.ToString(),
                    UnixTimestamp = attr.UpdatedUtc.ToUtcDateTimeOffset().ToUnixTimeMilliseconds(),
                    SignalQualityCode = SIGNAL_QUALITY_CODE_GOOD,
                    ValueBoolean = TimeSeries.ParseTimeseriesBoolean(value.ToString()),
                    Value = isCount ? 1 : valueDouble,
                    DateTime = attr.UpdatedUtc
                };
            })
            .Where(x => x != null);
            return (data, data.Count());
        }
    }
}
