using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Device.Application.Asset.Command.Model;
using Device.Application.Service.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using AHI.Infrastructure.MultiTenancy.Abstraction;

namespace Device.Application.Asset.Command.Handler
{
    class ValidateAssetAttributesSeriesRequestHandler : IRequestHandler<ValidateAssetAttributesSeries, IEnumerable<ValidateAssetAttributesDto>>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantContext _tenantContext;
        public ValidateAssetAttributesSeriesRequestHandler(IServiceScopeFactory scopeFactory, ITenantContext tenantContext)
        {
            _scopeFactory = scopeFactory;
            _tenantContext = tenantContext;
        }

        public async Task<IEnumerable<ValidateAssetAttributesDto>> Handle(ValidateAssetAttributesSeries request, CancellationToken cancellationToken)
        {
            var requestAssetsGroupTasks = request.ValidateAssets
                   .GroupBy(x => new { x.ProjectId, x.SubscriptionId })
                   .Select(x =>
                   {
                       var scope = _scopeFactory.CreateScope();
                       var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
                       tenantContext.SetProjectId(x.Key.ProjectId);
                       tenantContext.SetSubscriptionId(x.Key.SubscriptionId);
                       tenantContext.SetTenantId(_tenantContext.TenantId);
                       var assetAttributeService = scope.ServiceProvider.GetRequiredService<IAssetAttributeService>();
                       return x.Distinct().Select(item =>
                       {
                           return assetAttributeService.ValidateAssetAttributesSeriesAsync(item, cancellationToken);
                       });
                   });
            var tasks = requestAssetsGroupTasks.SelectMany(x => x);
            var result = await Task.WhenAll(tasks);
            var values = result.SelectMany(x => x);

            return request.ValidateAssets.Select(x =>
            {
                var assetId = x.AssetId;
                var validationAttributes = values.Where(asset => x.AssetId == asset.AssetId).SelectMany(s => s.AttributeIds);
                var isDifferent = x.AttributeIds.Any(a => !validationAttributes.Contains(a));
                return new ValidateAssetAttributesDto
                {
                    AssetId = assetId,
                    AttributeIds = x.AttributeIds,
                    Statics = x.Statics,
                    IsValid = (validationAttributes.Any() && !isDifferent) ? true : false
                };
            });
        }
    }
}