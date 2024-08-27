using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class DeviceTemplate : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool Deleted { get; set; }
        public int TotalMetric { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public virtual ICollection<TemplatePayload> Payloads { get; set; }
        public virtual ICollection<TemplateBinding> Bindings { get; set; }
        public virtual ICollection<Device> Devices { get; set; }
        public virtual ICollection<AssetAttributeDynamicTemplate> AssetAttributeDynamicTemplates { get; set; }
        public virtual ICollection<AssetAttributeCommandTemplate> AssetAttributeCommandTemplates { get; set; }
        public virtual ICollection<EntityTagDb> EntityTags { get; set; }
        public string ResourcePath { get; set; }
        public string CreatedBy { get; set; }

        public DeviceTemplate()
        {
            Id = Guid.NewGuid();
            Deleted = false;
            Payloads = new List<TemplatePayload>();
            Bindings = new List<TemplateBinding>();
            Devices = new List<Device>();
            AssetAttributeDynamicTemplates = new List<AssetAttributeDynamicTemplate>();
            AssetAttributeCommandTemplates = new List<AssetAttributeCommandTemplate>();
            EntityTags ??= new List<EntityTagDb>();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
        }
    }
}
