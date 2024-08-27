using System;

namespace Device.Application.Model
{
    public class AttributeStandaloneMapping
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public Guid AssetAttributeTemplateId { get; set; }

        public AttributeStandaloneMapping()
        {
        }

        public AttributeStandaloneMapping(Guid id, Guid assetId, Guid templateId)
        {
            Id = id;
            AssetId = assetId;
            AssetAttributeTemplateId = templateId;
        }
    }
}
