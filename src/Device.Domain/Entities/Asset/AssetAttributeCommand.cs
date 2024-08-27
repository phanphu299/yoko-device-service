using System;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeCommand : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetAttributeId { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public string Value { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public Device Device { get; set; }
        public AssetAttribute AssetAttribute { get; set; }
        public bool Deleted { get; set; }
        public Guid RowVersion { get; set; }
        public int SequentialNumber { get; set; } = 1;
        public DateTime? Timestamp { get; set; }

        public AssetAttributeCommand()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            Value = string.Empty;
            RowVersion = Guid.NewGuid();
        }
    }
}