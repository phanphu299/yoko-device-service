using System;

namespace Device.Domain.Entity
{
    public class FunctionBlockCategoryPath
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
        public string ParentName { get; set; }
        public int CategoryLevel { get; set; }
        public string CategoryPathId { get; set; }
        public string CategoryPathName { get; set; }
    }
}
