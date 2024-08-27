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
using AHI.Infrastructure.Exception.Helper;
using Device.ApplicationExtension.Extension;
using AHI.Infrastructure.SharedKernel.Abstraction;

namespace Device.Application.Historical.Query.Handler
{
    public class PaginationGetAssetAttributeSeriesHandler : IRequestHandler<PaginationGetAssetAttributeSeries, List<PaginationHistoricalDataDto>>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantContext _tenantContext;
        private readonly IMediator _mediator;
        private readonly ILoggerAdapter<PaginationGetAssetAttributeSeriesHandler> _logger;

        public PaginationGetAssetAttributeSeriesHandler(IServiceScopeFactory scopeFactory, ITenantContext tenantContext, IMediator mediator, ILoggerAdapter<PaginationGetAssetAttributeSeriesHandler> logger)
        {
            _scopeFactory = scopeFactory;
            _tenantContext = tenantContext;
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// az: https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/69021/
        /// Paging will follow timestamp of first attribute.
        /// If first attribute dont have data so all attribute will be empty series.
        /// This function will get first attibute series first then recalculate start/end for other attribute query.
        /// After get all other attribute values we will map timestamp sync with first attibute.
        /// 
        /// Ex: request 3 attribute: [att_id_1,att_id_2, att_id_3]
        /// start => end : 1/1/2021 => 10/1/2021
        /// 
        /// request 1: [att_id_1] -> series: [3/1/2021: 1,...., 8/1/2021: 7]
        /// 
        /// new start => new end : 3/1/2021 => 8/1/2021 
        /// request 2: [att_id_2, att_id_3] -> series: [4/1/2021: 10,...., 8/1/2021: 20]
        /// 
        /// Then left join timestamp from request 1 with timestamp request 2.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<List<PaginationHistoricalDataDto>> Handle(PaginationGetAssetAttributeSeries request, CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow;
            _logger.LogInformation($"CorrelationId: {request.ActivityId} | widgetId: {request.WidgetId} | PaginationGetAssetAttributeSeriesHandler - Start Handle at {DateTime.UtcNow} | pageIndex: {request.PageIndex}");
            ValidateRequest(request);

            _logger.LogInformation($"CorrelationId: {request.ActivityId} | widgetId: {request.WidgetId} | PaginationGetAssetAttributeSeriesHandler - End ValidateRequest after {DateTime.UtcNow.Subtract(start).TotalMilliseconds} | pageIndex: {request.PageIndex}");
            start = DateTime.UtcNow;

            var firstRequestAsset = request.Assets.First();
            var firstAttributeId = firstRequestAsset.AttributeIds.First();

            PaginationHistoricalDataDto paginationFirstAttributeData = null;
            using (var scope = _scopeFactory.CreateScope())
            {
                var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                // set new projectId into tenantContext
                tenantContext.RetrieveFromString(_tenantContext.TenantId, firstRequestAsset.SubscriptionId, firstRequestAsset.ProjectId);
                var seriesRequest = PaginationGetHistoricalData.Create(firstRequestAsset, request);
                seriesRequest.ActivityId = request.ActivityId;
                seriesRequest.WidgetId = request.WidgetId;

                var assetTimeSeriesService = scope.ServiceProvider.GetRequiredService<IAssetTimeSeriesService>();
                paginationFirstAttributeData = await assetTimeSeriesService.PaginationGetTimeSeriesDataAsync(seriesRequest, cancellationToken);

                _logger.LogInformation($"CorrelationId: {request.ActivityId} | widgetId: {request.WidgetId} | PaginationGetAssetAttributeSeriesHandler - End assetTimeSeriesService.PaginationGetTimeSeriesDataAsync after {DateTime.UtcNow.Subtract(start).TotalMilliseconds} | pageIndex: {request.PageIndex}");
                start = DateTime.UtcNow;
            }

            var firstRequestStart = firstRequestAsset.Start;
            var firstRequestEnd = firstRequestAsset.End;

            //Update all start/end other asset follow first asset
            request.Assets = request.Assets.Select(x =>
            {
                x.Start = firstRequestStart;
                x.End = firstRequestEnd;
                return x;
            });

            //init data fallback incase first attribute data null or empty
            long newStart = 0;
            long newEnd = 0;
            var pagingHitoricalData = new List<PaginationHistoricalDataDto>();
            var firstAttributeData = new PaginationAttributeDto
            {
                Series = new List<TimeSeriesDto>(),
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                TotalCount = 0
            };

            GetAssetAttributeSeries newCommand = GetAssetAttributeSeries.Create(request);
            newCommand.ActivityId = request.ActivityId;
            newCommand.WidgetId = request.WidgetId;

            _logger.LogInformation($"CorrelationId: {request.ActivityId} | widgetId: {request.WidgetId} | PaginationGetAssetAttributeSeriesHandler - End GetAssetAttributeSeries.Create after {DateTime.UtcNow.Subtract(start).TotalMilliseconds} | pageIndex: {request.PageIndex}");
            start = DateTime.UtcNow;

            //If first attribute has data, get new timestamp (follow first attribute timestamp result) and add filter next query for relate attribute.
            if (paginationFirstAttributeData != null && paginationFirstAttributeData.Attributes != null && paginationFirstAttributeData.Attributes.Any())
            {
                var firstAttribute = paginationFirstAttributeData.Attributes.First();
                if (firstAttribute.Series != null && firstAttribute.Series.Any())
                {
                    firstAttributeData = firstAttribute;
                    newCommand.Assets = newCommand.Assets.Where(x => !x.AttributeIds.Contains(firstAttributeId));
                    pagingHitoricalData.Add(paginationFirstAttributeData);
                    if (request.Assets.Count() > 1)
                    {
                        CalculateNewStartEnd(firstAttributeData, firstRequestAsset, firstRequestStart, firstRequestEnd,
                                     out newStart, out newEnd);
                    }
                }
            }

            _logger.LogInformation($"CorrelationId: {request.ActivityId} | widgetId: {request.WidgetId} | PaginationGetAssetAttributeSeriesHandler - End CalculateNewStartEnd after {DateTime.UtcNow.Subtract(start).TotalMilliseconds} | pageIndex: {request.PageIndex}");
            start = DateTime.UtcNow;

            if (request.Assets.Count() > 1)
            {
                newCommand.Assets = newCommand.Assets.Select(x =>
                {
                    x.Start = newStart;
                    x.End = newEnd;
                    return x;
                });

                var newQueryData = await _mediator.Send(newCommand);

                _logger.LogInformation($"CorrelationId: {request.ActivityId} | widgetId: {request.WidgetId} | PaginationGetAssetAttributeSeriesHandler - End handling command GetAssetAttributeSeries, total newQueryData: {newQueryData.Count} after {DateTime.UtcNow.Subtract(start).TotalMilliseconds} | pageIndex: {request.PageIndex}");
                start = DateTime.UtcNow;

                List<PaginationHistoricalDataDto> newPagingHitoricalData = new List<PaginationHistoricalDataDto>();

                var tasks = newQueryData.Select(item => Task.Run(() =>
                {
                    PaginationHistoricalDataDto pagingHistorical = PaginationHistoricalDataDto.Create(item);
                    _logger.LogInformation($"CorrelationId: {request.ActivityId} | widgetId: {request.WidgetId} | PaginationGetAssetAttributeSeriesHandler - End foreach (var item in newQueryData) PaginationHistoricalDataDto.Create after {DateTime.UtcNow.Subtract(start).TotalMilliseconds} | pageIndex: {request.PageIndex}");
                    start = DateTime.UtcNow;

                    pagingHistorical.Attributes = pagingHistorical.Attributes.Select(x =>
                    {
                        x.Series = x.Series.Where(s => firstAttributeData.Series.Exists(f => f.ts == s.ts)).ToList();
                        x.PageIndex = firstAttributeData.PageIndex;
                        x.PageSize = firstAttributeData.PageSize;
                        x.TotalCount = firstAttributeData.TotalCount;
                        return x;
                    }).ToList();

                    _logger.LogInformation($"CorrelationId: {request.ActivityId} | widgetId: {request.WidgetId} | PaginationGetAssetAttributeSeriesHandler - End foreach (var item in newQueryData) pagingHistorical.Attributes.Select after {DateTime.UtcNow.Subtract(start).TotalMilliseconds} | pageIndex: {request.PageIndex}");
                    start = DateTime.UtcNow;

                    pagingHistorical.Start = firstRequestStart;
                    pagingHistorical.End = firstRequestEnd;
                    newPagingHitoricalData.Add(pagingHistorical);
                }));

                await Task.WhenAll(tasks);

                _logger.LogInformation($"CorrelationId: {request.ActivityId} | widgetId: {request.WidgetId} | PaginationGetAssetAttributeSeriesHandler - End newPagingHitoricalData after {DateTime.UtcNow.Subtract(start).TotalMilliseconds} | pageIndex: {request.PageIndex}");
                start = DateTime.UtcNow;

                pagingHitoricalData.AddRange(newPagingHitoricalData);

                _logger.LogInformation($"CorrelationId: {request.ActivityId} | widgetId: {request.WidgetId} | PaginationGetAssetAttributeSeriesHandler - End AddRange pagingHitoricalData after {DateTime.UtcNow.Subtract(start).TotalMilliseconds} | pageIndex: {request.PageIndex}");
                start = DateTime.UtcNow;
            }

            var rs = (
                        from requestAsset in request.Assets
                        join timeseries in pagingHitoricalData on new { requestAsset.AssetId, requestAsset.Start, requestAsset.End, requestAsset.TimeGrain, requestAsset.RequestType, requestAsset.Aggregate } equals new { timeseries.AssetId, timeseries.Start, timeseries.End, timeseries.TimeGrain, RequestType = timeseries.QueryType, timeseries.Aggregate } into gj
                        let asset = gj.FirstOrDefault()
                        select new PaginationHistoricalDataDto()
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

            _logger.LogInformation($"CorrelationId: {request.ActivityId} | widgetId: {request.WidgetId} | PaginationGetAssetAttributeSeriesHandler - End Handle at {DateTime.UtcNow} after {DateTime.UtcNow.Subtract(start).TotalMilliseconds} | pageIndex: {request.PageIndex}");
            return rs;
        }

        private void CalculateNewStartEnd(PaginationAttributeDto firstAttributeData, AssetAttributeSeries firstRequestAsset,
                                          long firstRequestStart, long firstRequestEnd,
                                          out long newStart, out long newEnd)
        {
            newStart = firstAttributeData.Series.OrderBy(x => x.ts).First().ts;
            newEnd = firstAttributeData.Series.OrderByDescending(x => x.ts).First().ts;

            if (string.IsNullOrEmpty(firstRequestAsset.TimeGrain))
            {
                var startSubtract1Second = newStart - 1000;
                var endAdd1Second = newEnd + 1000;
                newStart = startSubtract1Second < firstRequestStart ? firstRequestStart : startSubtract1Second;
                newEnd = endAdd1Second > firstRequestEnd ? firstRequestEnd : endAdd1Second;
            }
            else
            {
                var startSubtractTimegrain = newStart - firstRequestAsset.TimeGrain.GetUnixTimestampPadding();
                var endAddTimegrain = newEnd + firstRequestAsset.TimeGrain.GetUnixTimestampPadding();
                newStart = startSubtractTimegrain < firstRequestStart ? firstRequestStart : startSubtractTimegrain;
                newEnd = endAddTimegrain > firstRequestEnd ? firstRequestEnd : endAddTimegrain;
            }
        }

        private void ValidateRequest(PaginationGetAssetAttributeSeries request)
        {
            if (!request.BePaging || request.PageIndex < 0 || request.PageSize <= 0)
            {
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(request.BePaging), ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
            }

            var firstAsset = request.Assets?.FirstOrDefault();
            if (firstAsset == null)
            {
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(request.Assets), ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
            }

            var firstAttributeId = firstAsset.AttributeIds?.FirstOrDefault();
            if (firstAttributeId == null)
            {
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(firstAsset.AttributeIds), ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
            }

            var requestCount = request.Assets.Count();

            var timeSeriesCount = request.Assets.Count(x => x.RequestType == HistoricalDataType.SERIES);
            if (timeSeriesCount > 0 && timeSeriesCount != requestCount)
            {
                var customTimeRangeRequestCount = request.Assets.Count(x => x.UseCustomTimeRange);
                // historical snapshot will be available for the first time, you can not combine with timeseries or another request.
                throw new SystemNotSupportedException($"Timeseries should not combine with another request type. Expected {customTimeRangeRequestCount} - Actual {requestCount}");
            }
        }
    }
}