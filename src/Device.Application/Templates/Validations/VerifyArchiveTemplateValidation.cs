using AHI.Infrastructure.Exception;
using Device.Application.Template.Command.Model;
using FluentValidation;
using System.Linq;

namespace Device.Application.Template.Validation
{
    public class VerifyArchiveTemplateValidation : AbstractValidator<ArchiveTemplateDto>
    {
        public VerifyArchiveTemplateValidation()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);

            RuleForEach(x => x.Bindings)
                .ChildRules(binding =>
                {
                    binding.RuleFor(x => x.Key).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                    binding.RuleFor(x => x.DefaultValue).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                    binding.RuleFor(x => x.DataType).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                });

            RuleFor(x => x.Payloads)
                .Must(x => x == null || x.Any())
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);

            RuleForEach(x => x.Payloads)
                .ChildRules(payload =>
                {
                    payload.RuleFor(x => x.JsonPayload).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                    payload.RuleForEach(x => x.Details)
                                .ChildRules(detail =>
                                {
                                    detail.RuleFor(x => x.Key).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                                    detail.RuleFor(x => x.Name).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                                    detail.RuleFor(x => x.DataType).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                                    detail.RuleFor(x => x.KeyTypeId).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                                });
                });
        }
    }
}
