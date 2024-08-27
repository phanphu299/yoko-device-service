using AHI.Infrastructure.Exception;
using Device.Application.Service;
using Device.Application.Uom.Command.Model;
using FluentValidation;

namespace Device.Application.Uom
{
    public class VerifyArchiveUomValidation : AbstractValidator<ArchiveUomDto>
    {
        public VerifyArchiveUomValidation()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                .MaximumLength(ValidationConstraints.MaximumUomNameLength)
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH);

            RuleFor(x => x.Abbreviation)
                .NotEmpty()
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                .MaximumLength(ValidationConstraints.MaximumUomAbbreviationLength)
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH);

            RuleFor(x => x.LookupCode)
                .NotEmpty()
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                .MaximumLength(ValidationConstraints.MaximumLookupCodeLength)
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH);

            RuleFor(x => x.RefFactor)
                .Must(rf => 0 <= rf && rf <= double.MaxValue)
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_OUT_OF_RANGE);

            RuleFor(x => x.RefOffset)
                .Must(ro => double.MinValue <= ro && ro <= double.MaxValue)
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_OUT_OF_RANGE);

            RuleFor(x => x.CanonicalFactor)
                .Must(rf => 0 <= rf && rf <= double.MaxValue)
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_OUT_OF_RANGE);

            RuleFor(x => x.CanonicalOffset)
                .Must(ro => double.MinValue <= ro && ro <= double.MaxValue)
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_OUT_OF_RANGE);
        }
    }
}
