using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class Uom : IEntity<int>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double? RefFactor { get; set; }
        public double? RefOffset { get; set; }
        public string LookupCode { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
        public bool Deleted { set; get; }
        public double? CanonicalFactor { get; set; }
        public double? CanonicalOffset { get; set; }
        public string Description { get; set; }
        public string Abbreviation { get; set; }
        public int? RefId { get; set; }
        public bool System { get; set; }
        public string CreatedBy { get; set; }
        public string ResourcePath { get; set; }
        public virtual Uom RefUom { get; set; }
        public virtual IEnumerable<Uom> Children { get; set; } = new List<Uom>();
        public virtual IEnumerable<AssetAttribute> AssetAttributes { get; set; }
        public virtual IEnumerable<AssetAttributeTemplate> AssetAttributeTemplates { get; set; }
        public virtual ICollection<EntityTagDb> EntityTags { get; set; }

        public Uom()
        {
            RefFactor = CanonicalFactor = 1;
            RefOffset = CanonicalOffset = 0;
            AssetAttributes = new List<AssetAttribute>();
            AssetAttributeTemplates = new List<AssetAttributeTemplate>();
            EntityTags ??= new List<EntityTagDb>();
        }
    }
}
