
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class AddDeviceRequestHandler : IRequestHandler<AddDevice, AddDeviceDto>
    {
        private readonly IDeviceService _service;
        public AddDeviceRequestHandler(IDeviceService service)
        {
            _service = service;
        }

        public Task<AddDeviceDto> Handle(AddDevice request, CancellationToken cancellationToken)
        {
            // persist to database and implement extra logic
            return _service.AddAsync(request, cancellationToken);
        }
    }
}
