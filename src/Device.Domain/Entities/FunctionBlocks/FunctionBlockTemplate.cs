using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class FunctionBlockTemplate : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { set; get; }
        public string DesignContent { set; get; }
        public string Content { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public bool Deleted { get; set; }
        public string ResourcePath { get; set; }
        public string CreatedBy { get; set; }
        public string TriggerType { get; set; }
        public string TriggerContent { get; set; }
        public Guid Version { get; set; }
        public ICollection<FunctionBlockExecution> Executions { get; set; }
        public virtual ICollection<FunctionBlockTemplateNode> Nodes { get; set; }
        public FunctionBlockTemplate()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            Nodes = new List<FunctionBlockTemplateNode>();
            Version = Guid.NewGuid();
        }
    }
}
