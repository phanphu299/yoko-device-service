using System;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class FunctionBlockBinding : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        public Guid FunctionBlockId { get; set; }
        public string DataType { get; set; }
        public string DefaultValue { get; set; }
        public string BindingType { get; set; }
        public string Description { get; set; }
        // public Guid? AssetTemplateId { get; set; }
        // public Guid? AttributeTemplateId { get; set; }
        public bool Deleted { get; set; }
        //public bool IsInput { get; set; }
        public DateTime CreatedUtc { get; set; }
        public int SequentialNumber { get; set; } = 1;
        public bool System { get; set; }
        public virtual FunctionBlock FunctionBlock { get; set; }
    }
}
