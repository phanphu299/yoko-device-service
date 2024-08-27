using AHI.Infrastructure.Exception;
using Device.Application.BlockTemplate.Query;
using FluentValidation;

namespace Device.Application.BlockTemplate.Validation
{
    public class BaseEditFunctionBlockTemplateValidation : AbstractValidator<BaseEditFunctionBlockTemplate>
    {
        public BaseEditFunctionBlockTemplateValidation()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.DesignContent).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.TriggerContent).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.TriggerType).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
        }
    }
}
