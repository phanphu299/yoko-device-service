using AHI.Infrastructure.Exception;
using Device.Application.Block.Command.Model;
using FluentValidation;

namespace Device.Application.Blocks.Validations
{
    public class VerifyArchiveFunctionBlockValidation : AbstractValidator<ArchiveFunctionBlockDto>
    {
        public VerifyArchiveFunctionBlockValidation()
        {
            RuleFor(x => x.Id).NotEmpty().WithErrorCode(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.Name).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.CategoryId).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            // RuleFor(x => x.Bindings).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);

            RuleForEach(x => x.Bindings).ChildRules(x =>
            {
                x.RuleFor(x => x.Id).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                x.RuleFor(x => x.Key).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                x.RuleFor(x => x.DataType).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                x.RuleFor(x => x.FunctionBlockId).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            });
        }
    }
}
