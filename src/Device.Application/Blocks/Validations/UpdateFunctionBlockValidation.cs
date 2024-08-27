using AHI.Infrastructure.Exception;
using Device.Application.Block.Command;
using FluentValidation;

namespace Device.Application.Blocks.Validations
{
    public class UpdateFunctionBlockValidation : AbstractValidator<UpdateFunctionBlock>
    {
        public UpdateFunctionBlockValidation()
        {
            RuleFor(x => x.Name).NotEmpty().WithErrorCode(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.CategoryId).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
        }
    }
}
