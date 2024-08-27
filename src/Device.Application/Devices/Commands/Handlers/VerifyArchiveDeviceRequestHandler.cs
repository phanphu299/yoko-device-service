using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Device.Application.Device.Command.Handler
{
    public class VerifyArchiveDeviceRequestHandler : IRequestHandler<VerifyDevice, BaseResponse>
    {
        private readonly IDeviceService _service;

        public VerifyArchiveDeviceRequestHandler(IDeviceService service)
        {
            _service = service;
        }

        public Task<BaseResponse> Handle(VerifyDevice request, CancellationToken cancellationToken)
        {
            return _service.VerifyArchiveAsync(request, cancellationToken);
        }
    }
}

