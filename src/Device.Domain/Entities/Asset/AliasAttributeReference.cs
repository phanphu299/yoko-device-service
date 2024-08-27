using System;

namespace Device.Domain.Entity
{
    public class AliasAttributeReference
    {
        public Guid TargetAttributeId { get; set; }
        public Guid TargetAssetId { get; set; }
        public Guid AssetId { get; set; }
        public Guid AttributeId { get; set; }
        public string DataType { get; set; }
        public string AttributeType { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public Uom Uom { get; set; }
        public int? UomId { get; set; }
        public int? DecimalPlace { get; set; }
        public bool? ThousandSeparator { get; set; }
        public string AliasAssetName { get; set; }
        public string AliasAttributeName { get; set; }
    }
}