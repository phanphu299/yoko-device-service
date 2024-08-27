using System;

namespace Device.Application.BlockFunction.Trigger.Model
{
    public class AssetAttributeTriggerDto : BlockExecutionTriggerDto
    {
        public Guid? AssetId { get; set; }
        public string AssetMarkup { get; set; }
        public string AttributeName { get; set; }
        public Guid? AttributeId { get; set; }
    }
}