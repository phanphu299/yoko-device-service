using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class GetDeviceMetricByIdRequestHandler : IRequestHandler<GetDeviceById, GetDeviceDto>
    {
        private readonly IDeviceService _service;
        public GetDeviceMetricByIdRequestHandler(IDeviceService service)
        {
            _service = service;
        }

        public Task<GetDeviceDto> Handle(GetDeviceById request, CancellationToken cancellationToken)
        {
            return _service.FindByIdAsync(request, cancellationToken);
        }
    }
}
