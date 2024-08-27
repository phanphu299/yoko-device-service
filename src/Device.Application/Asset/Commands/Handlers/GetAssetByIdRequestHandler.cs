using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.UserContext.Service.Abstraction;
using Device.Application.Asset.Command.Model;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    class GetAssetByIdRequestHandler : IRequestHandler<GetAssetById, GetAssetDto>
    {
        private readonly IAssetService _service;
        private readonly ISecurityService _securityService;
        public GetAssetByIdRequestHandler(IAssetService service, ISecurityService securityService)
        {
            _service = service;
            _securityService = securityService;
        }

        public async Task<GetAssetDto> Handle(GetAssetById request, CancellationToken cancellationToken)
        {
            var assetDto = await _service.FindAssetByIdOptimizedAsync(request, cancellationToken);
            if (request.AuthorizeUserAccess)
            {
                _securityService.AuthorizeAccess(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, Privileges.Asset.Rights.READ_ASSET, assetDto.ResourcePath, assetDto.CreatedBy);

                if (request.AuthorizeAssetAttributeAccess)
                {
                    _securityService.AuthorizeAccess(ApplicationInformation.APPLICATION_ID, Privileges.AssetAttribute.ENTITY_NAME, Privileges.AssetAttribute.Rights.READ_ASSET_ATTRIBUTE, assetDto.ResourcePath, assetDto.CreatedBy, includeRoleBase: true);
                }
            }
            return assetDto;
        }
    }
}
