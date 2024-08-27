using System.Threading;
using AHI.Infrastructure.UserContext.Service.Abstraction;
using Device.Application.Asset.Command.Model;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    public class GenerateAssetAttributeAssemblyHandler : IRequestHandler<GenerateAssetAttributeAssembly, AttributeAssemblyDto>
    {
        private readonly IAssetAssemblyService _assetAssemblyService;
        private readonly ISecurityService _securityService;
        private readonly IAssetService _assetService;

        public GenerateAssetAttributeAssemblyHandler(IAssetAssemblyService assetAssemblyService, ISecurityService securityService, IAssetService assetService)
        {
            _assetAssemblyService = assetAssemblyService;
            _securityService = securityService;
            _assetService = assetService;
        }
        public async System.Threading.Tasks.Task<AttributeAssemblyDto> Handle(GenerateAssetAttributeAssembly request, CancellationToken cancellationToken)
        {
            var assetDto = await _assetService.FindAssetByIdAsync(new GetAssetById(request.AssetId), cancellationToken);
            _securityService.AuthorizeAccess(ApplicationInformation.APPLICATION_ID, Privileges.AssetAttribute.ENTITY_NAME, Privileges.AssetAttribute.Rights.READ_ASSET_ATTRIBUTE, assetDto.ResourcePath, assetDto.CreatedBy);
            return await _assetAssemblyService.GenerateAssemblyAsync(request.AssetId, cancellationToken);
        }
    }
}