using System;
using AHI.Infrastructure.SharedKernel.Extension;
using Device.Consumer.KraftShared.Extensions;

namespace Device.Consumer.KraftShared.Model
{
    public class MetricValueLite
    {
        public string Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
    public class MetricValue
    {
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public long UnixTimestamp { get; set; }
        public DateTime Timestamp { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }

        public MetricValue(string deviceId, string metricKey, long unixTimestamp, DateTime timestamp, string value, string dataType)
        {
            DeviceId = deviceId;
            MetricKey = metricKey;
            UnixTimestamp = unixTimestamp;
            Timestamp = timestamp;
            Value = value;
            DataType = dataType;
        }

        public MetricValue(string deviceId, string metricKey, long unixTimestamp, string value, string dataType)
        {
            DeviceId = deviceId;
            MetricKey = metricKey;
            UnixTimestamp = unixTimestamp;
            Timestamp = unixTimestamp.ToString().CutOffFloatingPointPlace().UnixTimeStampToDateTime().CutOffNanoseconds();
            Value = value;
            DataType = dataType;
        }

        public MetricValue() { }
    }
}
