using System;

namespace Device.Consumer.KraftShared.Model
{
    public class AssetRuntimeTrigger
    {
        public Guid AssetId { get; set; }
        public Guid AttributeId { get; set; }
        public Guid? TriggerAssetId { get; set; }
        public Guid? TriggerAttributeId { get; set; }
        public string MetricKey { get; set; }
    }
}