using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Asset.Command.Model;
using Device.Application.Enum;
using Device.Application.Validation;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    public class ValidateAssetAttributesRequestHandler : IRequestHandler<ValidateAssetAttributeList, ValidateAssetAttributeListResponse>
    {
        private readonly IDictionary<ValidationType, IAttributeValidator> _validatorHandler;

        public ValidateAssetAttributesRequestHandler(IDictionary<ValidationType, IAttributeValidator> validatorHandler)
        {
            _validatorHandler = validatorHandler;
        }

        public async Task<ValidateAssetAttributeListResponse> Handle(ValidateAssetAttributeList request, CancellationToken cancellationToken)
        {
            var validationResponse = new ValidateAssetAttributeListResponse();
            if (_validatorHandler.ContainsKey(request.ValidationType))
            {
                var validator = _validatorHandler[request.ValidationType];
                var failures = await validator.ValidateAsync(request.Attribute.AssetId, request.Attribute, request.Attributes, request.ValidationAction);

                if (failures.Any())
                {
                    var errors = failures.Select(x => new ErrorField(x.PropertyName, x.ErrorMessage, x.FormattedMessagePlaceholderValues));
                    validationResponse.Properties = errors;
                }
            }

            return validationResponse;
        }
    }
}
