using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class FunctionBlockCategory : IEntity<Guid>
    {
        public FunctionBlockCategory()
        {
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            FunctionBlocks = new List<FunctionBlock>();
            Children = new List<FunctionBlockCategory>();
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
        public bool Deleted { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public bool System { get; set; }
        public virtual FunctionBlockCategory Parent { get; set; }
        public virtual ICollection<FunctionBlock> FunctionBlocks { get; set; }
        public virtual ICollection<FunctionBlockCategory> Children { get; set; }
    }
}
