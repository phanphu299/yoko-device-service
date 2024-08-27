using System.Threading;
using System.Threading.Tasks;
using Device.Application.AssetAttribute.Command;
using Device.Application.Template.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;
namespace Device.Application.Template.Command.Handler
{
    public class GetTemplatesByCriteriaRequestHandler : IRequestHandler<GetValidTemplatesByCriteria, BaseSearchResponse<GetValidTemplateDto>>
    {
        private readonly IValidTemplateService _service;
        public GetTemplatesByCriteriaRequestHandler(IValidTemplateService service)
        {
            _service = service;
        }

        public async Task<BaseSearchResponse<GetValidTemplateDto>> Handle(GetValidTemplatesByCriteria request, CancellationToken cancellationToken)
        {
            return await _service.SearchAsync(request);
        }
    }
}
