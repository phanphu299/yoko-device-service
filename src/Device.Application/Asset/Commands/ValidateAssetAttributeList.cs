using System.Collections.Generic;
using Device.Application.Asset.Command.Model;
using Device.Application.Enum;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class ValidateAssetAttributeList : IRequest<ValidateAssetAttributeListResponse>
    {
        public ValidationType ValidationType { get; set; } = ValidationType.Asset;
        public ValidationAction ValidationAction { get; set; } = ValidationAction.Upsert;
        public ValidatAttributeRequest Attribute { get; set; }

        /// <summary>
        /// This one used for validating runtime attribute
        /// </summary>
        public IEnumerable<ValidatAttributeRequest> Attributes { get; set; }

        public ValidateAssetAttributeList()
        {
        }
    }
}
