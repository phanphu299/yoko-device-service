using System;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeRuntimeTrigger : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public Guid AttributeId { get; set; }
        public Guid TriggerAssetId { get; set; }
        public Guid TriggerAttributeId { get; set; }
        public bool IsSelected { get; set; }
        public AssetAttributeRuntime AssetAttributeRuntime { get; set; }
        public AssetAttributeRuntimeMapping AssetAttributeRuntimeMapping { get; set; }
        public virtual Asset Asset { get; set; }
        public AssetAttributeRuntimeTrigger()
        {
            Id = Guid.NewGuid();
        }
    }
}
