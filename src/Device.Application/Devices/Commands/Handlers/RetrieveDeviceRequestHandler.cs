using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class RetrieveDeviceRequestHandler : IRequestHandler<RetrieveDevice, BaseResponse>
    {
        private readonly IDeviceService _service;

        public RetrieveDeviceRequestHandler(IDeviceService service)
        {
            _service = service;
        }

        public Task<BaseResponse> Handle(RetrieveDevice request, CancellationToken cancellationToken)
        {
            return _service.RetrieveAsync(request, cancellationToken);
        }
    }
}
