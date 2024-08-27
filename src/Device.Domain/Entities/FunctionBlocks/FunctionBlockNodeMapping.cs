using System;

namespace Device.Domain.Entity
{
    public class FunctionBlockNodeMapping
    {
        public Guid Id { get; set; }
        public Guid BlockExecutionId { get; set; }
        public FunctionBlockExecution BlockExecution { get; set; }
        /// <summary>
        /// If `BlockTemplateNodeId` is null, this entity's storing the using Asset Attribute of `FunctionBlockExecution`'s DiagramContent.
        /// </summary>
        public Guid? BlockTemplateNodeId { get; set; }
        public FunctionBlockTemplateNode BlockTemplateNode { get; set; }
        public string AssetMarkupName { get; set; }
        public Guid? AssetId { get; set; }
        public string AssetName { get; set; }
        public string TargetName { get; set; }
        public string Value { get; set; } // value can be asset attribute or asset table or primitive value, we don't know
        public FunctionBlockNodeMapping()
        {
            Id = Guid.NewGuid();
        }
    }
}
