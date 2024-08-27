using System;

namespace Device.Domain.ValueObject
{
    public class AssetDependency
    {
        public Guid AssetId { get; set; }
        public string AssetAttributeName { get; set; }
        public AssetDependency(Guid id, string name)
        {
            AssetId = id;
            AssetAttributeName = name;
        }
    }
}