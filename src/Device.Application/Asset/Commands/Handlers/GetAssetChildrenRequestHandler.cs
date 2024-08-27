using System.Threading;
using System.Threading.Tasks;
using Device.Application.Asset.Command.Model;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;
using Device.Application.Constant;
using AHI.Infrastructure.UserContext.Service.Abstraction;
using System;
using System.Linq;
using AHI.Infrastructure.Exception;

namespace Device.Application.Asset.Command.Handler
{
    public class GetAssetChildrenRequestHandler : IRequestHandler<GetAssetChildren, BaseSearchResponse<GetAssetSimpleDto>>
    {
        private readonly IAssetService _service;
        private readonly ISecurityService _securityService;
        private readonly ISecurityContext _securityContext;
        public GetAssetChildrenRequestHandler(IAssetService service, ISecurityService securityService, ISecurityContext securityContext)
        {
            _service = service;
            _securityService = securityService;
            _securityContext = securityContext;
        }

        public async Task<BaseSearchResponse<GetAssetSimpleDto>> Handle(GetAssetChildren request, CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow;
            var assetDto = await _service.FindAssetByIdOptimizedAsync(new GetAssetById(request.AssetId), cancellationToken);
            try
            {
                _securityService.AuthorizeAccess(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, Privileges.Asset.Rights.READ_ASSET, assetDto.ResourcePath, assetDto.CreatedBy);
            }
            catch (SystemSecurityException)
            {
                // should not throws the exception, simply return the empty array.
                return BaseSearchResponse<GetAssetSimpleDto>.CreateFrom(request, (long)DateTime.UtcNow.Subtract(start).TotalMilliseconds, 0, Array.Empty<GetAssetSimpleDto>());
            }
            _securityContext.Authorize(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, Privileges.Asset.Rights.READ_ASSET);
            var data = await _service.GetAssetChildrenAsync(request, cancellationToken);
            return BaseSearchResponse<GetAssetSimpleDto>.CreateFrom(request, (long)DateTime.UtcNow.Subtract(start).TotalMilliseconds, data.Count(), data);
        }
    }
}
