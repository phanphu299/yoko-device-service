using System;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttribute : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        // public bool EnabledExpression { get; set; }
        // public string Expression { get; set; }
        // public string ExpressionCompile { get; set; }
        public string AttributeType { get; set; }
        public string DataType { get; set; }
        //public int DataTypeId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public int? UomId { get; set; }
        public int? DecimalPlace { get; set; }
        public bool? ThousandSeparator { get; set; }
        public Asset Asset { get; set; }
        public Uom Uom { get; set; }
        public AssetAttributeAlias AssetAttributeAlias { get; set; }
        public AssetAttributeDynamic AssetAttributeDynamic { get; set; }
        public AssetAttributeIntegration AssetAttributeIntegration { get; set; }
        public AssetAttributeRuntime AssetAttributeRuntime { get; set; }
        public AssetAttributeCommand AssetAttributeCommand { get; set; }
        // public Guid? TriggerAssetId { get; set; }
        //public Guid? TriggerAttributeId { get; set; }
        public int SequentialNumber { get; set; } = 1;
        public AssetAttribute()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            // AliasAssetAttribute = new List<AssetAttributeAlias>();
        }
    }
}
