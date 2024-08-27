using System;

namespace Device.Consumer.KraftShared.Service.Model
{
    public class AssetAttributeCalculation
    {
        public Guid AssetId { get; set; }
        public Guid? AttributeId { get; set; }
        public Guid? AttributeTemplateId { get; set; }
    }
}
