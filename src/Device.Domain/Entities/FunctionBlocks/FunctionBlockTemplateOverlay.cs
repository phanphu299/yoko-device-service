using System;

namespace Device.Domain.Entity
{
    public class FunctionBlockTemplateOverlay
    {
        public Guid FunctionBlockExecutionId { get; set; }
        public FunctionBlockExecution FunctionBlockExecution { get; set; }
        public string Name { get; set; }
    }
}
