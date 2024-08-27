using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class UpdateDeviceRequestHandler : IRequestHandler<UpdateDevice, UpdateDeviceDto>
    {
        private readonly IDeviceService _service;
        public UpdateDeviceRequestHandler(IDeviceService service)
        {
            _service = service;
        }

        public Task<UpdateDeviceDto> Handle(UpdateDevice request, CancellationToken cancellationToken)
        {
            // persist to database and implement extra logic
            return _service.UpdateAsync(request, cancellationToken);
        }
    }
}
