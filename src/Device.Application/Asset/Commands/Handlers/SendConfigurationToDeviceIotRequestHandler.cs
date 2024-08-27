using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.UserContext.Service.Abstraction;
using Device.Application.Asset.Command.Model;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    public class SendConfigurationToDeviceIotRequestHandler : IRequestHandler<SendConfigurationToDeviceIot, SendConfigurationResultDto>
    {
        private readonly IAssetService _service;
        private readonly ISecurityService _securityService;
        public SendConfigurationToDeviceIotRequestHandler(IAssetService service, ISecurityService securityService)
        {
            _service = service;
            _securityService = securityService;
        }
        public async Task<SendConfigurationResultDto> Handle(SendConfigurationToDeviceIot request, CancellationToken cancellationToken)
        {
            var assetDto = await _service.FindAssetByIdAsync(new GetAssetById(request.AssetId), cancellationToken);
            _securityService.AuthorizeAccess(ApplicationInformation.APPLICATION_ID, Privileges.AssetAttribute.ENTITY_NAME, Privileges.AssetAttribute.Rights.WRITE_ASSET_ATTRIBUTE, assetDto.ResourcePath, assetDto.CreatedBy);
            // _ = _securityService.ValidateUserContextWithResource(Privileges.AssetAttribute.ENTITY_NAME, Privileges.AssetAttribute.Rights.WRITE_ASSET_ATTRIBUTE);
            // var requiredEntityPrivileges = new List<KeyValuePair<string, string>>
            // {
            //     new KeyValuePair<string, string>(Privileges.Asset.ENTITY_NAME, Privileges.Asset.Rights.WRITE_ASSET),
            //     new KeyValuePair<string, string>(Privileges.AssetAttribute.ENTITY_NAME, Privileges.AssetAttribute.Rights.WRITE_ASSET_ATTRIBUTE)
            // };
            //await _service.CheckUserRightPermissionAsync(request.AssetId, requiredEntityPrivileges: requiredEntityPrivileges, true, cancellationToken);
            return await _service.SendConfigurationToDeviceIotAsync(request, cancellationToken);
        }
    }
}
