using System.Threading;
using System.Threading.Tasks;
using Device.Application.Asset.Command.Model;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;
using Device.Application.Constant;
using AHI.Infrastructure.UserContext.Service.Abstraction;

namespace Device.Application.Asset.Command.Handler
{
    public class GetAssetHierarchyRequestHandler : IRequestHandler<GetAssetHierarchy, BaseSearchResponse<GetAssetHierarchyDto>>
    {
        private readonly IAssetService _service;
        private readonly ISecurityContext _securityService;
        public GetAssetHierarchyRequestHandler(IAssetService service, ISecurityContext securityService)
        {
            _service = service;
            _securityService = securityService;
        }
        public Task<BaseSearchResponse<GetAssetHierarchyDto>> Handle(GetAssetHierarchy request, CancellationToken cancellationToken)
        {
            _securityService.Authorize(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, Privileges.Asset.Rights.READ_ASSET);
            return _service.HierarchySearchAsync(request, cancellationToken);
        }
    }
}
