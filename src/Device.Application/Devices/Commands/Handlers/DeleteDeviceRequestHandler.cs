using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class DeleteDeviceRequestHandler : IRequestHandler<DeleteDevice, BaseResponse>
    {
        private readonly IDeviceService _service;
        public DeleteDeviceRequestHandler(IDeviceService service)
        {
            _service = service;
        }

        public Task<BaseResponse> Handle(DeleteDevice request, CancellationToken cancellationToken)
        {
            return _service.RemoveEntityForceAsync(request, cancellationToken);
        }
    }
}
