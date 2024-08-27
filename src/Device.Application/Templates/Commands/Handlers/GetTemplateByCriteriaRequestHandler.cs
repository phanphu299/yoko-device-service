using System.Threading;
using System.Threading.Tasks;
using Device.Application.Template.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.Template.Command.Handler
{
    public class GetTemplateByCriteriaRequestHandler : IRequestHandler<GetTemplateByCriteria, BaseSearchResponse<GetTemplateDto>>
    {
        private readonly IDeviceTemplateService _service;
        public GetTemplateByCriteriaRequestHandler(IDeviceTemplateService service)
        {
            _service = service;
        }

        public Task<BaseSearchResponse<GetTemplateDto>> Handle(GetTemplateByCriteria request, CancellationToken cancellationToken)
        {
            return _service.SearchAsync(request);
        }
    }
}
