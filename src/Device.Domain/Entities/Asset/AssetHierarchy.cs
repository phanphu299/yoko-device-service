using System;
using System.Collections.Generic;

namespace Device.Domain.Entity
{
    public class AssetHierarchy
    {
        public string AssetName { get; set; }
        public Guid AssetId { get; set; }
        public DateTime AssetCreatedUtc { get; set; }
        public bool AssetHasWarning { get; set; }
        public Guid? AssetTemplateId { get; set; }
        public int AssetRetentionDays { get; set; }
        public ICollection<Hierarchy> Hierarchy { get; set; }
        public Guid? ParentAssetId { get; set; }
        public bool IsFoundResult { get; set; }
        public AssetHierarchy()
        {
            Hierarchy = new List<Hierarchy>();
        }
    }

    public class Hierarchy
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool HasWarning { get; set; }
        public Guid? ParentAssetId { get; set; }
        public Guid? TemplateId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public int RetentionDays { get; set; }
        public bool IsFoundResult { get; set; }

        public static Hierarchy From(AssetHierarchy asset)
        {
            return new Hierarchy
            {
                CreatedUtc = asset.AssetCreatedUtc,
                HasWarning = asset.AssetHasWarning,
                Id = asset.AssetId,
                Name = asset.AssetName,
                ParentAssetId = asset.ParentAssetId,
                RetentionDays = asset.AssetRetentionDays,
                TemplateId = asset.AssetTemplateId,
                IsFoundResult = asset.IsFoundResult
            };
        }
    }
}
