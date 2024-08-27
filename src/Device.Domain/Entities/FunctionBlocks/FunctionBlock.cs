using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class FunctionBlock : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid CategoryId { get; set; }
        public string BlockContent { get; set; }
        public string Type { get; set; }
        // public bool IsActive { get; set; }
        public bool Deleted { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ResourcePath { get; set; }
        public bool System { get; set; }
        public Guid Version { get; set; }
        public ICollection<FunctionBlockBinding> Bindings { get; set; }
        public FunctionBlockCategory Categories { get; set; }
        public ICollection<FunctionBlockTemplateNode> BlockTemplateMappings { get; set; }
        public FunctionBlock()
        {
            Id = Guid.NewGuid();
            Bindings = new List<FunctionBlockBinding>();
        }
    }
}
