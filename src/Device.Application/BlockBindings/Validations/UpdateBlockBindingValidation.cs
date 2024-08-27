using AHI.Infrastructure.Exception;
using FluentValidation;

namespace Device.Application.BlockBinding.Validation
{
    public class UpdateBlockBindingValidation : AbstractValidator<UpdateFunctionBlockBinding>
    {
        public UpdateBlockBindingValidation()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.DefaultValue).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.DataType).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
        }
    }
}
