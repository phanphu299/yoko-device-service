using AHI.Infrastructure.Exception;
using Device.Application.BlockTemplate.Command.Model;
using FluentValidation;

namespace Device.Application.BlockTemplate.Validation
{
    public class VerifyArchiveBlockTemplateValidation : AbstractValidator<ArchiveBlockTemplateDto>
    {
        public VerifyArchiveBlockTemplateValidation()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                    .MaximumLength(255).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH);
            RuleFor(x => x.Content).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.DesignContent).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.TriggerType).MaximumLength(50).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH);
            RuleForEach(x => x.Nodes).ChildRules(node =>
            {
                node.RuleFor(x => x.Name).MaximumLength(255).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH);
                node.RuleFor(x => x.AssetMarkupName).MaximumLength(255).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH);
                node.RuleFor(x => x.TargetName).MaximumLength(255).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH);
                node.RuleFor(x => x.SequentialNumber).Must(num => num >= 0 && num <= int.MaxValue).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_OUT_OF_RANGE);
            });
        }
    }
}
