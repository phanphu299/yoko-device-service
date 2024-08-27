using System;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeCommandHistory : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetAttributeId { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public string Value { get; set; }
        public Guid RowVersion { get; set; }
        public AssetAttributeCommandHistory()
        {
            Id = Guid.NewGuid();
        }
    }
}