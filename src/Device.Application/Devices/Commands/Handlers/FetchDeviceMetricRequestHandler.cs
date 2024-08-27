using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class FetchDeviceMetricRequestHandler : IRequestHandler<FetchDeviceMetric, FetchDeviceMetricDto>
    {
        private readonly IDeviceService _service;

        public FetchDeviceMetricRequestHandler(IDeviceService service)
        {
            _service = service;
        }

        public Task<FetchDeviceMetricDto> Handle(FetchDeviceMetric request, CancellationToken cancellationToken)
        {
            return _service.FetchDeviceMetricAsync(request.DeviceId, request.Id, request.MetricKey);
        }
    }
}
