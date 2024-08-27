using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeTemplateIntegration : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetAttributeTemplateId { get; set; }
        public string IntegrationMarkupName { get; set; }
        public Guid IntegrationId { get; set; }
        public string DeviceMarkupName { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public virtual AssetAttributeTemplate AssetAttribute { get; set; }
        public virtual IEnumerable<AssetAttributeIntegrationMapping> AssetAttributeIntegrationMappings { get; set; }
        public AssetAttributeTemplateIntegration()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            AssetAttributeIntegrationMappings = new List<AssetAttributeIntegrationMapping>();
        }
    }
}
