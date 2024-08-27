using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeCommandTemplate : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetAttributeTemplateId { get; set; }
        public Guid DeviceTemplateId { get; set; }
        public string MarkupName { get; set; }
        public string MetricKey { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public virtual AssetAttributeTemplate AssetAttributeTemplate { get; set; }
        public virtual ICollection<AssetAttributeCommandMapping> AssetAttributeCommandMappings { get; set; }
        public virtual DeviceTemplate DeviceTemplate { get; set; }
        public AssetAttributeCommandTemplate()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            AssetAttributeCommandMappings = new List<AssetAttributeCommandMapping>();
        }
    }
}