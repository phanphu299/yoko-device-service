using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.Device.Command.Handler
{
    public class CheckExistDeviceHandler : IRequestHandler<CheckExistDevice, BaseResponse>
    {
        private readonly IDeviceService _service;
        public CheckExistDeviceHandler(IDeviceService service)
        {
            _service = service;
        }

        public Task<BaseResponse> Handle(CheckExistDevice request, CancellationToken cancellationToken)
        {
            return _service.CheckExistDevicesAsync(request, cancellationToken);
        }
    }
}
