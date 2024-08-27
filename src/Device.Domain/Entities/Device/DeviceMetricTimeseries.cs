using System;

namespace Device.Domain.Entity
{
    public class DeviceMetricTimeseries
    {
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public double? Value { get; set; }
        public string ValueText { get; set; }
        public long UnixTimestamp { get; set; }
        public double? LastGoodValue { get; set; }
        public string LastGoodValueText { get; set; }
        public long LastGoodUnixTimestamp { get; set; }
        public int? SignalQualityCode { get; set; }
        public DateTime DateTime { get; set; }
    }
}
