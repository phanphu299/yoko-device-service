using System;
using System.Collections.Generic;

namespace Device.Application.Asset.Command.Model
{
    public class AssetTemplateAttributeValidationRequest
    {
        public Guid Id { get; set; }
        public string DataType { get; set; }
        public string Expression { get; set; }
        public string Value { get; set; }
        public string AttributeType { get; set; }
        public Guid? AliasAttributeId { get; set; }
        public IEnumerable<AssetTemplateAttributeValidationRequest> Attributes { get; set; } = new List<AssetTemplateAttributeValidationRequest>();
    }
}