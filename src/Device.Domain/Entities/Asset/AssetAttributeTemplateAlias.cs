using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeTemplateAlias : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetAttributeTemplateId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public virtual AssetAttributeTemplate AssetAttribute { get; set; }
        public virtual IEnumerable<AssetAttributeAliasMapping> AssetAttributeAliasMappings { get; set; }
        public AssetAttributeTemplateAlias()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            AssetAttributeAliasMappings = new List<AssetAttributeAliasMapping>();
        }
    }
}
