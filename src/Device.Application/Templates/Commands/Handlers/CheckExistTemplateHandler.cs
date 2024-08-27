using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.Template.Command.Handler
{
    public class CheckExistTemplateHandler : IRequestHandler<CheckExistTemplate, BaseResponse>
    {
        private readonly IDeviceTemplateService _service;
        public CheckExistTemplateHandler(IDeviceTemplateService service)
        {
            _service = service;
        }

        public Task<BaseResponse> Handle(CheckExistTemplate request, CancellationToken cancellationToken)
        {
            return _service.CheckExistDeviceTemplatesAsync(request, cancellationToken);
        }
    }
}
