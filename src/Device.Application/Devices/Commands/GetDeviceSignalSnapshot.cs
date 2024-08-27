using MediatR;
using Device.Application.Device.Command.Model;

namespace Device.Application.Device.Command
{
    public class GetDeviceSignalSnapshot : IRequest<DeviceSignalSnapshotDto>
    {
        public string DeviceId { get; set; }
        public string MetricId { get; set; }

        public GetDeviceSignalSnapshot(string deviceId, string metricId)
        {
            DeviceId = deviceId;
            MetricId = metricId;
        }
    }
}