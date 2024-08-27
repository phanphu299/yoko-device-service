using System;
using System.Collections.Generic;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class AssetAttributeTemplate : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid AssetTemplateId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string AttributeType { get; set; }
        public int? UomId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public string DataType { get; set; }
        public virtual Uom Uom { get; set; }
        public virtual AssetTemplate AssetTemplate { get; set; }
        public int? DecimalPlace { get; set; }
        public bool? ThousandSeparator { get; set; }
        public int SequentialNumber { get; set; } = 1;
        public AssetAttributeTemplateIntegration AssetAttributeIntegration { get; set; }
        public AssetAttributeDynamicTemplate AssetAttributeDynamic { get; set; }
        public AssetAttributeRuntimeTemplate AssetAttributeRuntime { get; set; }
        public AssetAttributeCommandTemplate AssetAttributeCommand { get; set; }

        public IEnumerable<AssetAttributeDynamicMapping> AssetAttributeDynamicMappings { get; set; }
        public IEnumerable<AssetAttributeStaticMapping> AssetAttributeStaticMappings { get; set; }
        public IEnumerable<AssetAttributeRuntimeMapping> AssetAttributeRuntimeMappings { get; set; }
        public IEnumerable<AssetAttributeIntegrationMapping> AssetAttributeIntegrationMappings { get; set; }
        public IEnumerable<AssetAttributeCommandMapping> AssetAttributeCommandMappings { get; set; }
        public IEnumerable<AssetAttributeAliasMapping> AssetAttributeAliasMappings { get; set; }

        public AssetAttributeTemplate()
        {
            Id = Guid.NewGuid();
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            AssetAttributeDynamicMappings = new List<AssetAttributeDynamicMapping>();
            AssetAttributeStaticMappings = new List<AssetAttributeStaticMapping>();
            AssetAttributeRuntimeMappings = new List<AssetAttributeRuntimeMapping>();
            AssetAttributeIntegrationMappings = new List<AssetAttributeIntegrationMapping>();
            AssetAttributeCommandMappings = new List<AssetAttributeCommandMapping>();
            AssetAttributeAliasMappings = new List<AssetAttributeAliasMapping>();
        }
    }
}
