using System;
using System.Collections.Generic;

namespace Device.Application.Asset.Command.Model
{
    public class ValidateAssetAttributesDto
    {
        public Guid AssetId { get; set; }
        public IEnumerable<Guid> AttributeIds { get; set; }
        public IDictionary<string, string> Statics { get; set; }
        public bool IsValid { get; set; }
        public ValidateAssetAttributesDto() { }
        public ValidateAssetAttributesDto(Guid assetId, IEnumerable<Guid> attributeIds)
        {
            AssetId = assetId;
            AttributeIds = attributeIds;
        }
    }
}