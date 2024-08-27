
using AHI.Infrastructure.Exception;
using Device.Application.TemplateBinding.Command;
using FluentValidation;

namespace Device.Application.TemplateBinding.Validation
{
    public class UpdateTemplateBindingValidation : AbstractValidator<UpdateTemplateBinding>
    {
        public UpdateTemplateBindingValidation()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.DefaultValue).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.DataType).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
        }
    }
}