using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeRuntime : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetAttributeId { get; set; }
        // public Guid? TriggerAssetId { get; set; }
        // public Guid? TriggerAttributeId { get; set; }
        public bool IsTriggerVisibility { get; set; }
        public bool EnabledExpression { get; set; }
        public string Expression { get; set; }
        public string ExpressionCompile { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public virtual AssetAttribute AssetAttribute { get; set; }

        /// <summary>
        /// Including all Attribute from `Expression` and `Trigger Attribute`  objects
        /// </summary>
        public ICollection<AssetAttributeRuntimeTrigger> Triggers { get; set; }
        public AssetAttributeRuntime()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            Triggers = new List<AssetAttributeRuntimeTrigger>();
        }
    }
}
