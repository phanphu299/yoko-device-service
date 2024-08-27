using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class FetchDeviceRequestHandler : IRequestHandler<FetchDevice, GetDeviceDto>
    {
        private readonly IDeviceService _service;

        public FetchDeviceRequestHandler(IDeviceService service)
        {
            _service = service;
        }

        public Task<GetDeviceDto> Handle(FetchDevice request, CancellationToken cancellationToken)
        {
            return _service.FetchAsync(request.Id);
        }
    }
}