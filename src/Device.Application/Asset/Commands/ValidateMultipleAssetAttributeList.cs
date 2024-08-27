using System.Collections.Generic;
using Device.Application.Asset.Command.Model;
using Device.Application.Enum;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class ValidateMultipleAssetAttributeList : IRequest<List<ValidateMultipleAssetAttributeListResponse>>
    {
        public ValidationType ValidationType { get; set; } = ValidationType.Asset;

        public ValidationAction ValidationAction { get; set; } = ValidationAction.Upsert;

        public int StartIndex { get; set; }

        public int BatchSize { get; set; }

        public IEnumerable<ValidatAttributeRequest> Attributes { get; set; }

        public ValidateMultipleAssetAttributeList()
        {
        }
    }
}
