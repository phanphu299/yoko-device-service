using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Application.Analytic.Query.Model;
using Device.Application.Service.Abstraction;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Device.Application.Analytic.Query.Handler
{
    public class GetAssetAttributeStatisticsHandler : IRequestHandler<GetAssetAttributeStatisticsData, IEnumerable<AssetStatisticsDataDto>>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantContext _tenantContext;
        private readonly IConfiguration _configuration;
        public GetAssetAttributeStatisticsHandler(IServiceScopeFactory scopeFactory, ITenantContext tenantContext, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _tenantContext = tenantContext;
            _configuration = configuration;
        }

        public async Task<IEnumerable<AssetStatisticsDataDto>> Handle(GetAssetAttributeStatisticsData request, CancellationToken cancellationToken)
        {
            var requestAssetsGroup = request.Assets
                    .GroupBy(x => new { x.RequestType, x.Aggregate, x.TimeGrain, x.Start, x.End, x.ProjectId, x.SubscriptionId, x.AssetId, x.UseCustomTimeRange, x.GapfillFunction, x.PageSize })
                    .Select(
                    x => new AssetAttributeStatisticsData
                    {
                        TimeoutInSecond = request.TimeoutInSecond,
                        RequestType = x.Key.RequestType,
                        Aggregate = x.Key.Aggregate,
                        TimeGrain = x.Key.TimeGrain,
                        Start = x.Key.Start,
                        End = x.Key.End,
                        TimezoneOffset = request.TimezoneOffset,
                        ProjectId = x.Key.ProjectId,
                        SubscriptionId = x.Key.SubscriptionId,
                        UseCustomTimeRange = x.Key.UseCustomTimeRange,
                        AssetId = x.Key.AssetId,
                        AttributeIds = x.SelectMany(s => s.AttributeIds),
                        GapfillFunction = x.Key.GapfillFunction,
                        PageSize = x.Key.PageSize,
                    });
            var ret = new AssetAttributeStatisticsDataDto();

            // New change: we do not allow to mix the request, snapshot, historical snapshot and timeries should be separate request to BE.
            //ValidateRequest(requestAssetsGroup);

            //Get device client
            var tasks = requestAssetsGroup.Select(x =>
            {
                var scope = _scopeFactory.CreateScope();
                var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                // set new projectId into tenantContext
                tenantContext.SetProjectId(x.ProjectId);
                tenantContext.SetSubscriptionId(x.SubscriptionId);
                tenantContext.SetTenantId(_tenantContext.TenantId);

                //var seriesRequest = new GetHistoricalData(x.Start, x.End, x.TimeGrain, x.Aggregate, request.TimeoutInSecond, x.AssetId, x.AttributeIds, x.UseCustomTimeRange, x.RequestType, request.TimezoneOffset, x.GapfillFunction);
                var assetAnalyticService = scope.ServiceProvider.GetRequiredService<IAssetAnalyticService>();
                return assetAnalyticService.GetStatisticsDataAsync(x, cancellationToken);
            });
            var values = await Task.WhenAll(tasks);


            var rs = (
                    from requestAsset in request.Assets
                    join timeseries in values on new { requestAsset.AssetId, requestAsset.Start, requestAsset.End, requestAsset.TimeGrain, requestAsset.Aggregate } equals
                                                new { timeseries.AssetId, timeseries.Start, timeseries.End, timeseries.TimeGrain, timeseries.Aggregate } into gj
                    let asset = gj.FirstOrDefault()
                    let attributes = gj.SelectMany(g => g.Attributes).Where(metric => requestAsset.AttributeIds.Contains(metric.AttributeId) && metric.GapfillFunction == requestAsset.GapfillFunction)
                    //let distributionsAttributes = attributes.Select(x => AssetAttributeStatisticsDataDto.Create(x, x.Distributions.Mean, x.Distributions.StdDev))

                    select new AssetStatisticsDataDto()
                    {
                        AssetId = requestAsset.AssetId,
                        AssetName = asset?.AssetName,
                        AssetNormalizeName = asset?.AssetNormalizeName,
                        Start = requestAsset.Start,
                        End = requestAsset.End,
                        TimeGrain = requestAsset.TimeGrain,
                        Aggregate = requestAsset.Aggregate,
                        GapfillFunction = requestAsset.GapfillFunction,
                        Statics = requestAsset.Statics,
                        QueryType = requestAsset.RequestType,
                        TimezoneOffset = request.TimezoneOffset,
                        ProjectId = requestAsset.ProjectId,
                        RequestType = requestAsset.RequestType.ToString().ToLowerInvariant(),
                        SubscriptionId = requestAsset.SubscriptionId,
                        Attributes = gj.SelectMany(g => g.Attributes).Where(metric => requestAsset.AttributeIds.Contains(metric.AttributeId) && metric.GapfillFunction == requestAsset.GapfillFunction).Select(x => x)
                    }
                ).ToList();
            return rs;
        }

    }
}
