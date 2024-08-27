using System;

namespace Device.Application.Model
{
    public class ValidateAssetAttribute
    {
        public Guid AssetId { get; set; }
        public Guid AttributeId { get; set; }
    }
}
