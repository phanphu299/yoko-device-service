using System;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.UserContext.Service.Abstraction;
using Device.Application.Asset.Command.Model;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    public class UpsertAssetResquestHandler : IRequestHandler<UpsertAsset, UpsertAssetDto>
    {
        private readonly IAssetService _assetService;
        private readonly ISecurityService _securityService;

        public UpsertAssetResquestHandler(IAssetService assetService, ISecurityService securityService)
        {
            _assetService = assetService;
            _securityService = securityService;
        }

        public async Task<UpsertAssetDto> Handle(UpsertAsset request, CancellationToken cancellationToken)
        {
            await AuthorizeRequestAsync(request, cancellationToken);
            return await _assetService.UpsertAssetAsync(request, cancellationToken);
        }

        private async Task AuthorizeRequestAsync(UpsertAsset request, CancellationToken cancellationToken)
        {
            var document = request.Data;
            var operations = document.Operations;
            foreach (var operation in operations)
            {
                string path;
                switch (operation.op)
                {
                    case "add":
                    case "edit":
                    case "edit_parent":
                        path = operation.path.Trim('/');
                        if (Guid.TryParse(path, out var writeElementId))
                        {
                            var assetDto = await _assetService.FindAssetByIdAsync(new GetAssetById(writeElementId), cancellationToken);
                            _securityService.AuthorizeAccess(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, Privileges.Asset.Rights.WRITE_ASSET, assetDto.ResourcePath, assetDto.CreatedBy);
                        }
                        break;
                    case "remove":
                        path = operation.path.Trim('/');
                        if (Guid.TryParse(path, out var removeElementId))
                        {
                            var assetDto = await _assetService.FindAssetByIdAsync(new GetAssetById(removeElementId), cancellationToken);
                            _securityService.AuthorizeAccess(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, Privileges.Asset.Rights.DELETE_ASSET, assetDto.ResourcePath, assetDto.CreatedBy);
                        }
                        break;
                }
            }
        }
    }
}
