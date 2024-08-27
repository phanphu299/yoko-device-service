using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using MediatR;
using Device.Application.Device.Command.Model;
using System.Collections.Generic;

namespace Device.Application.Device.Command.Handler
{
    public class GetMectricsByDeviceIdRequestHandler : IRequestHandler<GetMetricsByDeviceId, IEnumerable<GetMetricsByDeviceIdDto>>
    {
        private readonly IDeviceService _service;
        public GetMectricsByDeviceIdRequestHandler(IDeviceService service)
        {
            _service = service;
        }

        public Task<IEnumerable<GetMetricsByDeviceIdDto>> Handle(GetMetricsByDeviceId request, CancellationToken cancellationToken)
        {
            return _service.GetMetricsByDeviceIdAsync(request, cancellationToken);
        }
    }
}