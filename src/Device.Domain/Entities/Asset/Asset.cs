using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class Asset : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? ParentAssetId { get; set; }
        public Guid? AssetTemplateId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public virtual ICollection<AssetAttribute> Attributes { get; set; }
        public virtual Asset ParentAsset { get; set; }
        public virtual ICollection<Asset> Children { get; set; }
        public virtual AssetTemplate AssetTemplate { get; set; }
        public virtual ICollection<AssetAttributeDynamicMapping> AssetAttributeDynamicMappings { get; set; }
        public virtual ICollection<AssetAttributeStaticMapping> AssetAttributeStaticMappings { get; set; }
        public virtual ICollection<AssetAttributeRuntimeMapping> AssetAttributeRuntimeMappings { get; set; }
        public virtual ICollection<AssetAttributeIntegrationMapping> AssetAttributeIntegrationMappings { get; set; }
        public virtual ICollection<AssetAttributeCommandMapping> AssetAttributeCommandMappings { get; set; }
        public virtual ICollection<AssetAttributeAliasMapping> AssetAttributeAliasMappings { get; set; }
        public virtual ICollection<EntityTagDb> EntityTags { get; set; }

        [NotMapped]
        public AssetWarning AssetWarning { get; set; }

        public int RetentionDays { get; set; }
        public string ResourcePath { get; set; }
        public string CreatedBy { get; set; }
        public bool IsDocument { get; set; }
        public virtual ICollection<AssetAttributeRuntimeTrigger> Triggers { get; set; }

        public Asset()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            Attributes = new List<AssetAttribute>();
            Children = new List<Asset>();
            AssetAttributeDynamicMappings = new List<AssetAttributeDynamicMapping>();
            AssetAttributeIntegrationMappings = new List<AssetAttributeIntegrationMapping>();
            AssetAttributeStaticMappings = new List<AssetAttributeStaticMapping>();
            AssetAttributeRuntimeMappings = new List<AssetAttributeRuntimeMapping>();
            AssetAttributeCommandMappings = new List<AssetAttributeCommandMapping>();
            AssetAttributeAliasMappings = new List<AssetAttributeAliasMapping>();
            Triggers = new List<AssetAttributeRuntimeTrigger>();
            RetentionDays = 90;
            EntityTags ??= new List<EntityTagDb>();
        }
    }
}
