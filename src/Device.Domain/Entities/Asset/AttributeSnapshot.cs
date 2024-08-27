using System;

namespace Device.Domain.Entity
{
    public class AttributeSnapshot
    {
        public Guid Id { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }
        public DateTime? Timestamp { get; set; }
        public Guid AssetId { get; set; }
        public string AttributeType { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }

    }
}
