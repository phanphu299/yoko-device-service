using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;

namespace Device.Application.Device.Command.Handler
{
    public class PushMessageToDeviceHandler : IRequestHandler<PushMessageToDevice, BaseResponse>
    {
        private readonly IDeviceService _service;

        public PushMessageToDeviceHandler(IDeviceService service)
        {
            _service = service;
        }

        public Task<BaseResponse> Handle(PushMessageToDevice request, CancellationToken cancellationToken)
        {
            return _service.PushConfigurationMessageAsync(request, cancellationToken);
        }
    }
}
