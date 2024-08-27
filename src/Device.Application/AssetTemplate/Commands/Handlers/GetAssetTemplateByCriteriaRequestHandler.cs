

using System.Threading;
using System.Threading.Tasks;
using Device.Application.AssetTemplate.Command.Model;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;
using Device.Application.Constant;
using AHI.Infrastructure.UserContext.Service.Abstraction;

namespace Device.Application.AssetTemplate.Command.Handler
{
    public class GetAssetTemplateByCriteriaRequestHandler : IRequestHandler<GetAssetTemplateByCriteria, BaseSearchResponse<GetAssetTemplateDto>>
    {
        private readonly IAssetTemplateService _service;
        private readonly ISecurityContext _securityContext;
        public GetAssetTemplateByCriteriaRequestHandler(
                IAssetTemplateService service,
                ISecurityContext securityContext
            )
        {
            _service = service;
            _securityContext = securityContext;
        }

        public async Task<BaseSearchResponse<GetAssetTemplateDto>> Handle(GetAssetTemplateByCriteria request, CancellationToken cancellationToken)
        {
            _securityContext.Authorize(ApplicationInformation.APPLICATION_ID, Privileges.Asset.ENTITY_NAME, Privileges.AssetTemplate.Rights.READ_ASSET_TEMPLATE);
            return await _service.SearchAsync(request);
        }
    }
}
