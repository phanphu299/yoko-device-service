using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class GenerateMetricAssemblyRequestHandler : IRequestHandler<GenerateDeviceMetricAssemply, MetricAssemblyDto>
    {
        private readonly IDeviceService _deviceService;
        public GenerateMetricAssemblyRequestHandler(IDeviceService deviceService)
        {
            _deviceService = deviceService;
        }
        public Task<MetricAssemblyDto> Handle(GenerateDeviceMetricAssemply request, CancellationToken cancellationToken)
        {
            return _deviceService.GenerateMetricAssemblyAsync(request.Id, cancellationToken);
        }
    }
}
