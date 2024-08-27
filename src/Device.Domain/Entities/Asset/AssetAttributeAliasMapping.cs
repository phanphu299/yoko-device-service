using System;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeAliasMapping : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public Guid? AliasAssetId { get; set; }
        public Guid? AliasAttributeId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public Guid AssetAttributeTemplateId { get; set; }
        public virtual AssetAttributeTemplate AssetAttributeTemplate { set; get; }
        public virtual Asset Asset { set; get; }

        public string AliasAssetName { get; set; }
        public string AliasAttributeName { get; set; }
        public string DataType { get; set; }
        public int? UomId { get; set; }
        public int? DecimalPlace { get; set; }
        public bool? ThousandSeparator { get; set; }
        
        public AssetAttributeAliasMapping()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
        }
    }
}