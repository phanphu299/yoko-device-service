using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Historical.Query.Model;
using Device.Application.Service.Abstraction;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.SharedKernel.Abstraction;
/*
IMPORTANCE: High performance api should be reviewed and approved by technical team.
PLEASE DO NOT CHANGE OR MODIFY THE LOGIC IF YOU DONT UNDERSTAND 
Author: Thanh Tran
Email: thanh.tran@yokogawa.com
*/
namespace Device.Application.Historical.Query.Handler
{
    public class GetAssetAttributeSeriesHandler : IRequestHandler<GetAssetAttributeSeries, List<HistoricalDataDto>>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantContext _tenantContext;
        private readonly ILoggerAdapter<GetAssetAttributeSeriesHandler> _logger;

        public GetAssetAttributeSeriesHandler(IServiceScopeFactory scopeFactory,
            ITenantContext tenantContext,
            ILoggerAdapter<GetAssetAttributeSeriesHandler> logger)
        {
            _scopeFactory = scopeFactory;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public async Task<List<HistoricalDataDto>> Handle(GetAssetAttributeSeries request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"CorrelationId: {request.ActivityId} | widgetId: {request.WidgetId} | GetAssetAttributeSeriesHandler - Start Handle");
            DateTime start = DateTime.UtcNow;

            List<AssetAttributeSeries> requestAssetsGroup = request.Assets
                    .GroupBy(x => new { x.RequestType, x.Aggregate, x.TimeGrain, x.Start, x.End, x.ProjectId, x.SubscriptionId, x.AssetId, x.UseCustomTimeRange, x.GapfillFunction, x.PageSize })
                    .Select(
                    x => new AssetAttributeSeries
                    {
                        TimeoutInSecond = request.TimeoutInSecond,
                        RequestType = x.Key.RequestType,
                        Aggregate = x.Key.Aggregate,
                        TimeGrain = x.Key.TimeGrain,
                        Start = x.Key.Start,
                        End = x.Key.End,
                        ProjectId = !string.IsNullOrEmpty(x.Key.ProjectId) ? x.Key.ProjectId : _tenantContext.ProjectId,
                        SubscriptionId = !string.IsNullOrEmpty(x.Key.SubscriptionId) ? x.Key.SubscriptionId : _tenantContext.SubscriptionId,
                        UseCustomTimeRange = x.Key.UseCustomTimeRange,
                        AssetId = x.Key.AssetId,
                        AttributeIds = x.SelectMany(s => s.AttributeIds),
                        GapfillFunction = x.Key.GapfillFunction,
                        PageSize = x.Key.PageSize
                    }).ToList();

            // New change: we do not allow to mix the request, snapshot, historical snapshot and timeries should be separate request to BE.
            ValidateRequest(requestAssetsGroup);

            _logger.LogInformation($"CorrelationId: {request.ActivityId} | widgetId: {request.WidgetId} | GetAssetAttributeSeriesHandler - End ValidateRequest, total requestAssetsGroup: {requestAssetsGroup.Count} after {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
            start = DateTime.UtcNow;

            //Get device client
            var tasks = requestAssetsGroup.Select(async x =>
               {
                   var scope = _scopeFactory.CreateScope();
                   var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                   // set new projectId into tenantContext
                   tenantContext.RetrieveFromString(_tenantContext.TenantId, x.SubscriptionId, x.ProjectId);
                   var seriesRequest = new GetHistoricalData(x.Start, x.End, x.TimeGrain, x.Aggregate, request.TimeoutInSecond, x.AssetId, x.AttributeIds, x.UseCustomTimeRange, x.RequestType, request.TimezoneOffset, x.GapfillFunction, x.PageSize, request.Quality);

                   if (x.RequestType == HistoricalDataType.SNAPSHOT && x.UseCustomTimeRange)
                   {
                       // for snapshot, let limit 1 in timeseries
                       // https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/1241/
                       var assetHistoricalSnapshotService = scope.ServiceProvider.GetRequiredService<IAssetHistoricalSnapshotService>();
                       return (await assetHistoricalSnapshotService.GetSnapshotDataAsync(seriesRequest, cancellationToken)).ToList();
                   }
                   else if (x.RequestType == HistoricalDataType.SNAPSHOT)
                   {
                       // https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/1241/
                       // snapshot can get the future data. consider the latest data between enddate
                       var assetSnapshotService = scope.ServiceProvider.GetRequiredService<IAssetSnapshotService>();
                       return (await assetSnapshotService.GetSnapshotDataAsync(seriesRequest, cancellationToken)).ToList();
                   }
                   else
                   {
                       var assetTimeSeriesService = scope.ServiceProvider.GetRequiredService<IAssetTimeSeriesService>();
                       return (await assetTimeSeriesService.GetTimeSeriesDataAsync(seriesRequest, cancellationToken)).ToList();
                   }
               });

            var result = await Task.WhenAll(tasks);

            _logger.LogInformation($"CorrelationId: {request.ActivityId} | widgetId: {request.WidgetId} | GetAssetAttributeSeriesHandler - End Task.WhenAll after {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
            start = DateTime.UtcNow;

            var values = result.SelectMany(x => x).ToList();

            var rs = (
                        from requestAsset in request.Assets
                        join timeseries in values on new { requestAsset.AssetId, requestAsset.Start, requestAsset.End, requestAsset.TimeGrain, requestAsset.RequestType, requestAsset.Aggregate } equals new { timeseries.AssetId, timeseries.Start, timeseries.End, timeseries.TimeGrain, RequestType = timeseries.QueryType, timeseries.Aggregate } into gj
                        let asset = gj.FirstOrDefault()
                        select new HistoricalDataDto()
                        {
                            AssetId = requestAsset.AssetId,
                            AssetName = asset?.AssetName,
                            AssetNormalizeName = asset?.AssetNormalizeName,
                            Start = requestAsset.Start,
                            End = requestAsset.End,
                            TimeGrain = requestAsset.TimeGrain,
                            Aggregate = requestAsset.Aggregate,
                            Statics = requestAsset.Statics,
                            QueryType = requestAsset.RequestType,
                            TimezoneOffset = request.TimezoneOffset,
                            RequestType = requestAsset.RequestType.ToString().ToLowerInvariant(),
                            Attributes = gj.SelectMany(g => g.Attributes).Where(metric => requestAsset.AttributeIds.Contains(metric.AttributeId) && metric.GapfillFunction == requestAsset.GapfillFunction).ToList()
                        }
            ).ToList();

            _logger.LogInformation($"CorrelationId: {request.ActivityId} | widgetId: {request.WidgetId} | GetAssetAttributeSeriesHandler - End Handle after {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");

            return rs;
        }

        private void ValidateRequest(IEnumerable<AssetAttributeSeries> requests)
        {
            var requestCount = requests.Count();
            var customTimeRangeRequestCount = requests.Count(x => x.UseCustomTimeRange);
            if (customTimeRangeRequestCount > 0 && customTimeRangeRequestCount != requestCount)
            {
                // historical snapshot will be available for the first time, you can not combine with timeseries or another request.
                throw new SystemNotSupportedException($"Historical snapshot should not combine with another request type. Expected {customTimeRangeRequestCount} - Actual {requestCount}");
            }
            var snapshotCount = requests.Count(x => x.RequestType == HistoricalDataType.SNAPSHOT);
            if (snapshotCount > 0 && snapshotCount != requestCount)
            {
                // historical snapshot will be available for the first time, you can not combine with timeseries or another request.
                throw new SystemNotSupportedException($"Snapshot should not combine with another request type. Expected {customTimeRangeRequestCount} - Actual {requestCount}");
            }

            var timeSeriesCount = requests.Count(x => x.RequestType == HistoricalDataType.SERIES);
            if (timeSeriesCount > 0 && timeSeriesCount != requestCount)
            {
                // historical snapshot will be available for the first time, you can not combine with timeseries or another request.
                throw new SystemNotSupportedException($"Timeseries should not combine with another request type. Expected {customTimeRangeRequestCount} - Actual {requestCount}");
            }
        }
    }
}