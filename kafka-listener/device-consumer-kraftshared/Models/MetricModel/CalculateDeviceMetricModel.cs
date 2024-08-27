using System;

namespace Device.Consumer.KraftShared.Models.MetricModel
{
    public class RuntimeValueObject
    {
        public DateTime Timestamp { get; set; }
        public Guid AssetId { get; set; }
        public Guid AttributeId { get; set; }
        public object Value { get; set; }
        public string DataType { get; set; }
        public int RetentionDays { get; set; }
    }

    public class DeviceMetricSeries
    {
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public object Value { get; set; }
        public int SignalQualityCode { get; set; }
        public int RetentionDays { get; set; }
    }

    public class DeviceAttributeSnapshot
    {
        public Guid AssetId { get; set; }
        public string? DeviceId { get; set; }
        public Guid AttributeId { get; set; }
        public string? AttributeType { get; set; }
        public object Value { get; set; }
        public string? DataType { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? MetricKey { get; set; }
        public int RetentionDays { get; set; }
    }

    public class AttributeSnapshot
    {
        public Guid AttributeId { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    public class MetricValuesObject
    {
        public DateTime Timestamp { get; set; }
        public string Value { get; set; }
        public Guid IntegrationId { get; set; }
        public string DeviceId { get; set; }
        public string MetricId { get; set; }
    }
}
