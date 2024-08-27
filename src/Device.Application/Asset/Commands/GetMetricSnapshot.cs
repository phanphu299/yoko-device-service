using System.Collections.Generic;
using Device.Application.Asset.Command.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class GetMetricSnapshot : IRequest<IEnumerable<SnapshotDto>>
    {
        public string DeviceId { get; set; }
        public GetMetricSnapshot(string deviceId)
        {
            DeviceId = deviceId;
        }
    }
}
