using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using MediatR;
using Device.Application.TemplateDetail.Command.Model;
using System.Collections.Generic;

namespace Device.Application.Template.Command.Handler
{
    public class GetTemplateMetricByTemplateIdRequestHandler : IRequestHandler<GetTemplateMetricsByTemplateId, IEnumerable<GetTemplateDetailsDto>>
    {
        private readonly IDeviceTemplateService _service;
        public GetTemplateMetricByTemplateIdRequestHandler(IDeviceTemplateService service)
        {
            _service = service;
        }

        public async Task<IEnumerable<GetTemplateDetailsDto>> Handle(GetTemplateMetricsByTemplateId request, CancellationToken cancellationToken)
        {
            return await _service.GetTemplateMetricsByTemplateIDAsync(request);
        }
    }
}
