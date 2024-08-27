using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using Device.Application.Models;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class ExportDeviceRequestHandler : IRequestHandler<ExportDevice, ActivityResponse>
    {
        private readonly IDeviceService _deviceService;

        public ExportDeviceRequestHandler(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }

        public Task<ActivityResponse> Handle(ExportDevice request, CancellationToken cancellationToken)
        {
            return _deviceService.ExportAsync(request, cancellationToken);
        }
    }
}
