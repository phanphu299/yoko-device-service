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
    public class GetAssetByCriteriaRequestHandler : IRequestHandler<GetAssetByCriteria, BaseSearchResponse<GetAssetSimpleDto>>
    {
        private readonly IAssetService _service;
        private readonly ISecurityContext _securityContext;

        public GetAssetByCriteriaRequestHandler(IAssetService service, ISecurityContext securityContext)
        {
            _service = service;
            _securityContext = securityContext;
        }

        public Task<BaseSearchResponse<GetAssetSimpleDto>> Handle(GetAssetByCriteria request, CancellationToken cancellationToken)
        {
            _securityContext.Authorize(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, Privileges.Asset.Rights.READ_ASSET);
            return _service.HierarchySearchWithSecurityAsync(request);
        }
    }
}
