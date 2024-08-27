using System;
using System.Collections.Generic;
using System.Linq;

namespace Device.Application.Asset.Command.Model
{
    public class ValidateMultipleAssetAttributeListResponse
    {
        public Guid AttributeId { get; set; }

        public bool IsSuccess => !Properties.Any();

        public IEnumerable<ErrorField> Properties { get; set; }

        public ValidateMultipleAssetAttributeListResponse()
        {
            Properties = new List<ErrorField>();
        }
    }
}
