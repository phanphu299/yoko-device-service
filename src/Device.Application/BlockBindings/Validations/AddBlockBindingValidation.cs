using AHI.Infrastructure.Exception;
using Device.Application.BlockBinding.Command;
using FluentValidation;

namespace Device.Application.BlockBinding.Validation
{
    public class AddBlockBindingValidation : AbstractValidator<AddFunctionBlockBinding>
    {
        public AddBlockBindingValidation()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED).MaximumLength(255).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH);
            RuleFor(x => x.DataType).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.Description).MaximumLength(255).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH);
        }
    }
}
