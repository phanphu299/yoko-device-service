using System;

namespace AHI.Device.Function.Service.Model
{
    public class AssetAttribute
    {
        public Guid AssetId { get; set; }
        public Guid AttributeId { get; set; }
        public Guid? AttributeTemplateId { get; set; }
        public string Expression { get; set; }
        public bool? EnabledExpression { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }
        public string AttributeType { get; set; }
        public DateTime? UpdatedUtc { get; set; }
        public Guid? TriggerAttributeId { get; set; }
        public Guid? TriggerAssetId { get; set; }
    }
}
