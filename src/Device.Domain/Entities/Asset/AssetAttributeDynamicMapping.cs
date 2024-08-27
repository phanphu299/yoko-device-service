using System;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeDynamicMapping : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public Guid AssetAttributeTemplateId { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public int SequentialNumber { get; set; } = 1;
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public Device Device { get; set; }
        public virtual Asset Asset { set; get; }
        public virtual AssetAttributeTemplate AssetAttributeTemplate { set; get; }
        public virtual AssetAttributeDynamicTemplate AssetAttributeDynamicTemplate { set; get; }

        public AssetAttributeDynamicMapping()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
        }
    }
}