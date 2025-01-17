using System;
using System.Collections.Generic;
using System.Linq;

namespace Device.Application.Asset.Command.Model
{
    public class ValidateAssetAttributeListResponse
    {
        public bool IsSuccess => !Properties.Any();

        public IEnumerable<ErrorField> Properties { get; set; }

        public ValidateAssetAttributeListResponse()
        {
            Properties = new List<ErrorField>();
        }
    }
}
