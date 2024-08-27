using System.Threading;
using System.Threading.Tasks;
using Device.Application.Template.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;
using System.Collections.Generic;

namespace Device.Application.Template.Command.Handler
{
    public class GetTemplateByDefaultRequestHandle : IRequestHandler<GetTemplateByDefault, IEnumerable<GetValidTemplateDto>>
    {
        private readonly IDeviceTemplateService _service;
        public GetTemplateByDefaultRequestHandle(IDeviceTemplateService service)
        {
            _service = service;
        }

        public Task<IEnumerable<GetValidTemplateDto>> Handle(GetTemplateByDefault request, CancellationToken cancellationToken)
        {
            return _service.FindAllEntityWithDefaultAsync(request, cancellationToken);
        }
    }
}
