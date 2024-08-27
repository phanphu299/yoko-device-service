using System;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.UserContext.Service.Abstraction;
using Device.Application.Asset.Command.Model;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;

namespace Device.Application.Asset.Command.Handler
{
    public class UpsertAssetAttributeResquestHandler : IRequestHandler<UpsertAssetAttribute, UpsertAssetAttributeDto>
    {

        private readonly IAssetAttributeService _service;
        private readonly IAssetService _assetService;
        private readonly ISecurityService _securityService;

        public UpsertAssetAttributeResquestHandler(IAssetAttributeService service, ISecurityService securityService, IAssetService assetService)
        {
            _service = service;
            _securityService = securityService;
            _assetService = assetService;
        }

        public virtual async Task<UpsertAssetAttributeDto> Handle(UpsertAssetAttribute request, CancellationToken cancellationToken)
        {
            //check permission : if not will throw
            await CheckUserRightPermissionAsync(request, cancellationToken);
            return await _service.UpsertAssetAttributeAsync(request, cancellationToken);
        }

        private async Task CheckUserRightPermissionAsync(UpsertAssetAttribute command, CancellationToken token)
        {
            JsonPatchDocument document = command.Data;
            var operations = document.Operations;
            foreach (var operation in operations)
            {
                var assetDto = await _assetService.FindAssetByIdAsync(new GetAssetById(command.AssetId), token);
                var path = operation.path.Trim('/');
                switch (operation.op)
                {
                    case "add":
                    case "edit":
                        if (Guid.TryParse(path, out var writeElementId))
                        {
                            _securityService.AuthorizeAccess(ApplicationInformation.APPLICATION_ID, Privileges.AssetAttribute.ENTITY_NAME, Privileges.AssetAttribute.Rights.WRITE_ASSET_ATTRIBUTE, assetDto.ResourcePath, assetDto.CreatedBy);
                        }
                        break;
                    case "remove":
                        if (Guid.TryParse(path, out var deleteElementId))
                        {
                            _securityService.AuthorizeAccess(ApplicationInformation.APPLICATION_ID, Privileges.AssetAttribute.ENTITY_NAME, Privileges.AssetAttribute.Rights.DELETE_ASSET_ATTRIBUTE, assetDto.ResourcePath, assetDto.CreatedBy);
                        }
                        break;
                }
            }
        }
    }
}
