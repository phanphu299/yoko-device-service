using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class PatchDeviceRequestHandler : IRequestHandler<PatchDevice, UpdateDeviceDto>
    {
        private readonly IDeviceService _service;
        public PatchDeviceRequestHandler(IDeviceService service)
        {
            _service = service;
        }

        public Task<UpdateDeviceDto> Handle(PatchDevice request, CancellationToken cancellationToken)
        {
            // persist to database and implement extra logic
            return _service.PartialUpdateAsync(request, cancellationToken);
        }
    }
}
