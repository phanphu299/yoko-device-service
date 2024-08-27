using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeRuntimeMapping : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public Guid AssetAttributeTemplateId { get; set; }
        public bool EnabledExpression { get; set; }
        public string Expression { get; set; }
        public string ExpressionCompile { get; set; }
        // public Guid? TriggerAssetId { get; set; }
        // public Guid? TriggerAttributeId { get; set; }
        public int SequentialNumber { get; set; } = 1;
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public virtual Asset Asset { set; get; }
        public virtual AssetAttributeTemplate AssetAttributeTemplate { set; get; }
        public virtual AssetAttributeRuntimeTemplate AssetAttributeRuntimeTemplate { set; get; }
        public bool IsTriggerVisibility { get; set; }
        public ICollection<AssetAttributeRuntimeTrigger> Triggers { get; set; }
        public AssetAttributeRuntimeMapping()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            Triggers = new List<AssetAttributeRuntimeTrigger>();
        }
    }
}
