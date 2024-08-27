using System;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeCommandMapping : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public Guid AssetAttributeTemplateId { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public string Value { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public int SequentialNumber { get; set; } = 1;
        public Guid RowVersion { get; set; }
        public DateTime? Timestamp { get; set; }
        public Device Device { get; set; }
        public virtual Asset Asset { get; set; }
        public virtual AssetAttributeTemplate AssetAttributeTemplate { get; set; }
        public virtual AssetAttributeCommandTemplate AssetAttributeCommandTemplate { get; set; }

        public AssetAttributeCommandMapping()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            Value = string.Empty;
            RowVersion = Guid.NewGuid();
        }
    }
}