using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Device.Application.Constant;
using Device.Application.Repository;
using MediatR;
namespace Device.Application.Device.Command.Handler
{
    public class GetDeviceByIntegrationIdRequestHandler : IRequestHandler<GetDeviceByIntegrationId, IEnumerable<string>>
    {
        private readonly IReadAssetRepository _readAssetRepository;
        private readonly IReadAssetAttributeRepository _readAssetAttributeRepository;

        public GetDeviceByIntegrationIdRequestHandler(
            IReadAssetRepository readAssetRepository,
            IReadAssetAttributeRepository readAssetAttributeRepository)
        {
            _readAssetRepository = readAssetRepository;
            _readAssetAttributeRepository = readAssetAttributeRepository;
        }

        public async Task<IEnumerable<string>> Handle(GetDeviceByIntegrationId request, CancellationToken cancellationToken)
        {
            var assetAttributes = await _readAssetAttributeRepository.AsQueryable().AsNoTracking().Where(x => x.AttributeType == AttributeTypeConstants.TYPE_INTEGRATION).Where(x => x.AssetAttributeIntegration.IntegrationId == request.IntegrationId).Select(x => x.AssetAttributeIntegration.DeviceId).ToListAsync();
            var assetAttributeMappings = await _readAssetRepository.OnlyAssetAsQueryable().AsNoTracking().Where(x => x.AssetAttributeIntegrationMappings.Any(a => a.IntegrationId == request.IntegrationId)).SelectMany(x => x.AssetAttributeIntegrationMappings.Select(x => x.DeviceId)).ToListAsync();
            return assetAttributes.Union(assetAttributeMappings).Distinct();
        }
    }
}