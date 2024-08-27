using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Service.Tag.Model;
using Device.Application.Models;
using Device.Application.Service;
using Device.Application.Uom.Command;
using FluentValidation;

namespace Device.Application.Uom
{
    public class AddUomValidation : AbstractValidator<AddUom>
    {
        public AddUomValidation()
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
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);

            // Factor: text box, placeholder: Factor, data type: float4, only allow input real number > 0, and <= 3.4E+38. Allowed charater: number, dot, comma
            RuleFor(x => x.RefFactor)
                .Must(x => 0 <= x.Value && x.Value <= double.MaxValue)
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_OUT_OF_RANGE)
                .When(x => x.RefFactor != null);

            // Offset: text box, placeholder: Offset: data type: float4, only allow input real number > 0 and <= 3.4E+38. Allowed charater: number, dot, comma
            RuleFor(x => x.RefOffset)
                .Must(x => double.MinValue <= x.Value && x.Value <= double.MaxValue)
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_OUT_OF_RANGE)
                .When(x => x.RefOffset != null);

            // text box, placeholder: Canonical Factor, data type: float4, only allow input real number > 0, and <= 3.4E+38. Allowed charater: number, dot, comma
            //RuleFor(x => x.CanonicalFactor)
            //    .Must(x => 0 <= x.Value && x.Value <= double.MaxValue)
            //    .WithMessage(x => $"0 <= {nameof(x.CanonicalFactor)} <= {double.MaxValue}")
            //    .WithErrorCode(ValidationMessageCodes.NumberOutOfRange)
            //    .When(x => x.CanonicalFactor != null);

            //// Canonical Offset: text box, placeholder: Canonical Offset: data type: float4, only allow input real number > 0 and <= 3.4E+38. Allowed charater: number, dot, comma
            //RuleFor(x => x.CanonicalOffset)
            //    .Must(x => 0 <= x.Value && x.Value <= double.MaxValue)
            //    .WithMessage(x => $"0 <= {nameof(x.CanonicalOffset)} <= {double.MaxValue}")
            //    .WithErrorCode(ValidationMessageCodes.NumberOutOfRange)
            //    .When(x => x.CanonicalOffset != null);

            RuleForEach(x => x.Tags).SetValidator(
                new InlineValidator<UpsertTag> {
                    agValidator => agValidator.RuleFor(x => x.Key)
                                              .NotEmpty()
                                              .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                                              .MaximumLength(216)
                                              .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH)
                                              .Must(ContainsInvalidChar)
                                              .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID),
                    agValidator => agValidator.RuleFor(x => x.Value)
                                              .NotEmpty()
                                              .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                                              .MaximumLength(216)
                                              .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH)
                                              .Must(ContainsInvalidChar)
                                              .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID)
                }
            );
        }

        private bool ContainsInvalidChar(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return true;
            return !input.Contains(':') && !input.Contains(';') && !input.Contains(',');
        }
    }
}
