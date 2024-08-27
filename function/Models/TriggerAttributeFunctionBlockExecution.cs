using System;

namespace AHI.Device.Function.Model
{
    public class TriggerAttributeFunctionBlockExecution
    {
        public Guid FunctionBlockId { get; set; }
        public Guid AssetId { get; set; }
        public Guid AssetAttributeId { get; set; }
    }
}