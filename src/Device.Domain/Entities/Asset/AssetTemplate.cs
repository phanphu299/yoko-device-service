using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetTemplate : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public virtual IEnumerable<AssetAttributeTemplate> Attributes { get; set; }
        public virtual IEnumerable<Asset> Assets { set; get; }
        public string ResourcePath { get; set; }
        public string CreatedBy { get; set; }
        public virtual ICollection<EntityTagDb> EntityTags { get; set; }
        public AssetTemplate()
        {
            Id = Guid.NewGuid();
            Attributes = new List<AssetAttributeTemplate>();
            Assets = new List<Asset>();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
        }
    }
}
