using System.Threading;
using MediatR;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using Device.Application.Asset.Command.Model;
using System.Collections.Generic;

namespace Device.Application.Asset.Command.Handler
{
    public class GetMetricSnapshotRequestHandler : IRequestHandler<GetMetricSnapshot, IEnumerable<SnapshotDto>>
    {
        private readonly IDeviceService _service;

        public GetMetricSnapshotRequestHandler(IDeviceService service)
        {
            _service = service;
        }

        public Task<IEnumerable<SnapshotDto>> Handle(GetMetricSnapshot request, CancellationToken cancellationToken)
        {
            return _service.GetMetricSnapshotAsync(request, cancellationToken);
        }
    }
}