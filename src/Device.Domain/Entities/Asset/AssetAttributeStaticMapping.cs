using System;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeStaticMapping : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public Guid AssetAttributeTemplateId { get; set; }
        public string Value { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public virtual Asset Asset { set; get; }
        public int SequentialNumber { get; set; } = 1;
        public bool IsOverridden { get; set; }
        public virtual AssetAttributeTemplate AssetAttributeTemplate { set; get; }
        public AssetAttributeStaticMapping()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
        }
    }
}
