using System;

namespace Device.Consumer.KraftShared.Model
{
    public class TriggerAttributeFunctionBlockExecution
    {
        public Guid FunctionBlockId { get; set; }
        public Guid AssetId { get; set; }
        public Guid AssetAttributeId { get; set; }
    }
}