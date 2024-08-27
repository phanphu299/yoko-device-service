using Device.Application.Device.Command.Model;
using MediatR;

namespace Device.Application.Device.Command
{
    public class FetchDeviceMetric : IRequest<FetchDeviceMetricDto>
    {
        public int Id { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }

        public FetchDeviceMetric(int id, string deviceId, string metricKey)
        {
            Id = id;
            DeviceId = deviceId;
            MetricKey = metricKey;
        }
    }
}
