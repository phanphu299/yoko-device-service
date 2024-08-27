using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeDynamicTemplate : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetAttributeTemplateId { get; set; }
        public Guid DeviceTemplateId { get; set; }
        public string MarkupName { get; set; }
        public string MetricKey { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public virtual AssetAttributeTemplate AssetAttribute { get; set; }
        public virtual IEnumerable<AssetAttributeDynamicMapping> AssetAttributeDynamicMappings { get; set; }
        public virtual DeviceTemplate Template { get; set; }

        public AssetAttributeDynamicTemplate()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            AssetAttributeDynamicMappings = new List<AssetAttributeDynamicMapping>();
        }
    }
}
