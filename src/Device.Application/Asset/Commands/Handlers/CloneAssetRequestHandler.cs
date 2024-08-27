
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using MediatR;
using Device.Application.Asset.Command.Model;
using Device.Application.Constant;
using AHI.Infrastructure.UserContext.Service.Abstraction;

namespace Device.Application.Asset.Command.Handler
{
    public class CloneAssetRequestHandler : IRequestHandler<GetAssetClone, GetAssetDto>
    {
        private readonly ISecurityService _securityService;
        private readonly IAssetService _assetService;
        public CloneAssetRequestHandler(IAssetService assetService, ISecurityService securityService)
        {
            _assetService = assetService;
            _securityService = securityService;
        }

        public async Task<GetAssetDto> Handle(GetAssetClone request, CancellationToken cancellationToken)
        {
            //check permission : if not will throw
            var assetDto = await _assetService.FindAssetByIdAsync(new GetAssetById(request.Id), cancellationToken);
            _securityService.AuthorizeAccess(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, Privileges.Asset.Rights.WRITE_ASSET, assetDto.ResourcePath, assetDto.CreatedBy);
            return await _assetService.GetAssetCloneAsync(request, cancellationToken);
        }
    }
}
