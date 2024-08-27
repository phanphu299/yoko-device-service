using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Application.Analytic.Query.Model;
using Device.Application.Service.Abstraction;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Device.Application.Analytic.Query.Handler
{
    public class GetAssetHistogramHandler : IRequestHandler<GetAssetAttributeHistogramData, IEnumerable<AssetHistogramDataDto>>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantContext _tenantContext;
        public GetAssetHistogramHandler(IServiceScopeFactory scopeFactory, ITenantContext tenantContext)
        {
            _scopeFactory = scopeFactory;
            _tenantContext = tenantContext;
        }

        public async Task<IEnumerable<AssetHistogramDataDto>> Handle(GetAssetAttributeHistogramData request, CancellationToken cancellationToken)
        {
            var requestAssetsGroup = request.Assets
                     .GroupBy(x => new { x.ProjectId, x.SubscriptionId, x.AssetId, x.BinSize, x.Start, x.End, x.Aggregate, x.TimeGrain, x.GapfillFunction })
                     .Select(
                     x => new AssetAttributeHistogramData
                     {
                         ProjectId = x.Key.ProjectId,
                         SubscriptionId = x.Key.SubscriptionId,
                         AssetId = x.Key.AssetId,
                         BinSize = x.Key.BinSize,
                         Start = x.Key.Start,
                         End = x.Key.End,
                         AttributeIds = x.SelectMany(s => s.AttributeIds),
                         Aggregate = x.Key.Aggregate,
                         TimeGrain = x.Key.TimeGrain,
                         GapfillFunction = x.Key.GapfillFunction,
                         TimezoneOffset = request.TimezoneOffset,
                         TimeoutInSecond = request.TimeoutInSecond
                     });
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
                   return assetAnalyticService.GetHistogramDataAsync(x, cancellationToken);
               });
            var values = await Task.WhenAll(tasks);
            var rs = (
                    from requestAsset in request.Assets
                    join timeseries in values on new { requestAsset.AssetId, requestAsset.Start, requestAsset.End, requestAsset.TimeGrain, requestAsset.Aggregate } equals
                                                new { timeseries.AssetId, timeseries.Start, timeseries.End, timeseries.TimeGrain, timeseries.Aggregate } into gj
                    let asset = gj.FirstOrDefault()
                    select new AssetHistogramDataDto()
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
                        TimezoneOffset = request.TimezoneOffset,
                        BinSize = requestAsset.BinSize,
                        ProjectId = requestAsset.ProjectId,
                        SubscriptionId = requestAsset.SubscriptionId,
                        Attributes = gj.SelectMany(g => g.Attributes).Where(metric => requestAsset.AttributeIds.Contains(metric.AttributeId) && metric.GapfillFunction == requestAsset.GapfillFunction).Select(x => x)
                    }
                ).ToList();
            return rs;
        }
    }
}
