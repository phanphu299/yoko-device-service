
using System;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class TemplateBinding : IEntity<int>
    {
        public int Id { get; set; }
        public Guid TemplateId { get; set; }
        public string Key { get; set; }
        public string DataType { get; set; }
        public string DefaultValue { get; set; }
        public virtual DeviceTemplate Template { get; set; }
        //public virtual DeviceBinding DeviceBinding { get; set; }
    }
}
