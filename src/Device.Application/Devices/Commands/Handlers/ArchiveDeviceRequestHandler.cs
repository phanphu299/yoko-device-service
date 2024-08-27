using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class ArchiveDeviceRequestHandler : IRequestHandler<ArchiveDevice, ArchiveDeviceDataDto>
    {
        private readonly IDeviceService _service;

        public ArchiveDeviceRequestHandler(IDeviceService service)
        {
            _service = service;
        }

        public async Task<ArchiveDeviceDataDto> Handle(ArchiveDevice request, CancellationToken cancellationToken)
        {
            var devices = await _service.ArchiveAsync(request, cancellationToken);
            var result = new ArchiveDeviceDataDto(devices);
            return result;
        }
    }
}
