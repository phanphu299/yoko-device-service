using System;

namespace AHI.Device.Function.Service.Model
{
    public class AssetAttributeAlias
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public Guid AssetAtrtributeId { get; set; }
        public int AliasAssetId { get; set; }
        public Guid? AliasAttributeId { get; set; }
        public Guid? AliasAttributeTemplateId { get; set; }
    }
}
