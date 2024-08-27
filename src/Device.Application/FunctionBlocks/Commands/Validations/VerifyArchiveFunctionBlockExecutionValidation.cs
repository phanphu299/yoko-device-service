using AHI.Infrastructure.Exception;
using Device.Application.BlockFunction.Model;
using FluentValidation;

namespace Device.Application.BlockFunction.Validations
{
    public class VerifyArchiveFunctionBlockExecutionValidation : AbstractValidator<ArchiveFunctionBlockExecutionDto>
    {
        public VerifyArchiveFunctionBlockExecutionValidation()
        {
            RuleFor(x => x.Id).NotEmpty().WithErrorCode(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.Name).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.TriggerType).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.TriggerContent).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.Status).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);

            RuleForEach(x => x.Mappings).ChildRules(x =>
            {
                x.RuleFor(x => x.Id).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                x.RuleFor(x => x.BlockExecutionId).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            });
        }
    }
}
