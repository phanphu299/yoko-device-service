using System.Collections.Generic;
using System;

namespace Device.Domain.Entity
{
    public class FunctionBlockCategoryHierarchy
    {
        public Guid EntityId { get; set; }
        public string EntityName { get; set; }
        public bool EntityIsCategory { get; set; }
        public string EntityType { get; set; }
        public virtual ICollection<CategoryHierarchy> Hierarchy { get; }
        public FunctionBlockCategoryHierarchy()
        {
            Hierarchy = new List<CategoryHierarchy>();
        }
    }
    public class CategoryHierarchy
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public bool IsCategory { get; set; }
        public string Type { get; set; }

    }
}
