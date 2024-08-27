using System;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeIntegration : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetAttributeId { get; set; }
        public Guid IntegrationId { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public virtual AssetAttribute AssetAttribute { get; set; }
        public AssetAttributeIntegration()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
        }
    }
}
