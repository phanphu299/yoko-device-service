using System;

namespace Device.Domain.Entity
{
    public class DeviceMetricSnapshot
    {
        public string DeviceId { get; set; }
        public string MetricId { get; set; }
        public string Value { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }
}