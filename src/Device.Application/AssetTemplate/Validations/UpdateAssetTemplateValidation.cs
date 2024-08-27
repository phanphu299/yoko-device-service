using AHI.Infrastructure.Exception;
using Device.Application.AssetTemplate.Command;
using FluentValidation;

namespace Device.Application.AssetTemplate.Validation
{
    public class UpdateAssetTemplateValidation : AbstractValidator<UpdateAssetTemplate>
    {
        public UpdateAssetTemplateValidation()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.Attributes).NotNull().NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
        }
    }
}