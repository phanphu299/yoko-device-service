using System;

namespace AHI.Device.Function.Model
{
    public class MetricSnapshot
    {
        public object Value { get; set; }
        public string MetricKey { get; set; }
        public string DeviceId { get; set; }
        public DateTime Timestamp { get; set; }
        public int RetentionDays { get; set; } = 90;
    }
}