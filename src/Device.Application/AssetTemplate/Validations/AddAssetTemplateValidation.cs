using FluentValidation;
using Device.Application.AssetTemplate.Command;
using AHI.Infrastructure.Exception;
using Device.Application.Constant;
using System.Linq;

namespace Device.Application.AssetTemplate.Validation
{
    public class AddAssetTemplateValidation : AbstractValidator<AddAssetTemplate>
    {
        public AddAssetTemplateValidation()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleForEach(x => x.Attributes).SetValidator(
               new InlineValidator<AssetTemplateAttribute> {
                    agValidator => agValidator.RuleFor(x => x.AttributeType)
                                              .NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                                              .Must(x => AttributeTypeConstants.ALLOWED_ATTRIBUTE_TYPES.Any(a => a == x)).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID)
               }
           );
        }
    }
}