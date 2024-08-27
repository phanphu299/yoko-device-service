using System;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeDynamic : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetAttributeId { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public Device Device { get; set; }
        public virtual AssetAttribute AssetAttribute { get; set; }

        public AssetAttributeDynamic()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
        }
    }
}