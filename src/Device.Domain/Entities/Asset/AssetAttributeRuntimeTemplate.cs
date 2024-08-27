using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeRuntimeTemplate : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetAttributeTemplateId { get; set; }
        // public string MarkupName { get; set; }
        //public Guid? TriggerAssetTemplateId { get; set; }
        public Guid? TriggerAttributeId { get; set; }
        public bool EnabledExpression { get; set; }
        public string Expression { get; set; }
        public string ExpressionCompile { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public virtual AssetAttributeTemplate AssetAttribute { get; set; }
        public virtual IEnumerable<AssetAttributeRuntimeMapping> AssetAttributeRuntimeMappings { get; set; }
        public AssetAttributeRuntimeTemplate()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            AssetAttributeRuntimeMappings = new List<AssetAttributeRuntimeMapping>();
        }
    }
}
