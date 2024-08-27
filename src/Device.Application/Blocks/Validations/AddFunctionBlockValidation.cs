using FluentValidation;
using Device.Application.Block.Command;
using AHI.Infrastructure.Exception;

namespace Device.Application.Block.Validation
{
    public class AddFunctionBlockValidation : AbstractValidator<AddFunctionBlock>
    {
        public AddFunctionBlockValidation()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.CategoryId).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
        }
    }
}
