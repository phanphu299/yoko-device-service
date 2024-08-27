using System.Threading;
using System.Threading.Tasks;
using Device.Application.Models;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Template.Command.Handler
{
    public class ExportDeviceTemplateRequestHandler : IRequestHandler<ExportDeviceTemplate, ActivityResponse>
    {
        private readonly IDeviceTemplateService _service;

        public ExportDeviceTemplateRequestHandler(IDeviceTemplateService service)
        {
            _service = service;
        }

        public Task<ActivityResponse> Handle(ExportDeviceTemplate request, CancellationToken cancellationToken)
        {
            return _service.ExportAsync(request, cancellationToken);
        }
    }
}
