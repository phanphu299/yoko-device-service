using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class RefreshTokenRequestHandler : IRequestHandler<RefreshToken, UpdateDeviceDto>
    {
        private readonly IDeviceService _service;
        public RefreshTokenRequestHandler(IDeviceService service)
        {
            _service = service;
        }

        public Task<UpdateDeviceDto> Handle(RefreshToken request, CancellationToken cancellationToken)
        {
            return _service.RefreshTokenAsync(request, cancellationToken);
        }
    }
}
