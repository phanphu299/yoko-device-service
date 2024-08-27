using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class FunctionBlockExecution : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? TemplateId { get; set; } // can be null
        public Guid? FunctionBlockId { get; set; }
        public string DiagramContent { get; set; }
        public bool Deleted { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
        public string TriggerType { get; set; }
        public string TriggerContent { get; set; }
        public string Status { get; set; } = "ST";
        public DateTime? ExecutedUtc { get; set; }
        public Guid? JobId { get; set; }
        public string ExecutionContent { get; set; }
        public bool RunImmediately { get; set; }
        public string ResourcePath { get; set; }
        public string CreatedBy { get; set; }
        public string TriggerAssetMarkup { get; set; }
        public Guid Version { get; set; }
        public Guid? TriggerAssetId { get; set; }
        public Guid? TriggerAttributeId { get; set; }
        public FunctionBlockTemplate Template { get; set; }
        public FunctionBlock FunctionBlock { get; set; }
        public FunctionBlockTemplateOverlay TemplateOverlay { get; set; }
        public ICollection<FunctionBlockNodeMapping> Mappings { get; set; }
        public FunctionBlockExecution()
        {
            Id = Guid.NewGuid();
            Mappings = new List<FunctionBlockNodeMapping>();
        }
    }
}
