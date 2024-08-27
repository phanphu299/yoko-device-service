using FluentValidation;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Repository;
using UomProperty = AHI.Device.Function.Constant.ErrorMessage.ErrorProperty.Uom;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;

namespace AHI.Device.Function.Model.ImportModel.Validation
{
    public class UomValidation : AbstractValidator<Uom>
    {
        private const double MinFactor = 0;
        private const double MaxFactor = double.MaxValue;
        private const double MinOffset = double.MinValue;
        private const double MaxOffset = double.MaxValue;
        public UomValidation(ISystemContext systemContext)
        {
            RuleFor(x => x.Lookup)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().WithName(UomProperty.LOOKUP).WithMessage(ValidationMessage.REQUIRED)
                .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                .SetValidator(new MatchRegex(RegexConfig.GENERAL_RULE, RegexConfig.GENERAL_DESCRIPTION, systemContext));

            RuleFor(x => x.Name)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().WithName(UomProperty.NAME).WithMessage(ValidationMessage.REQUIRED)
                .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                .SetValidator(new MatchRegex(RegexConfig.NAME_RULE, RegexConfig.NAME_DESCRIPTION, systemContext))
                .WithName(UomProperty.NAME).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE);

            RuleFor(x => x.Abbreviation)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().WithName(UomProperty.ABBREVIATION).WithMessage(ValidationMessage.REQUIRED)
                .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                .SetValidator(new MatchRegex(RegexConfig.GENERAL_RULE, RegexConfig.GENERAL_DESCRIPTION, systemContext));

            RuleFor(x => x.RefName)
                .MaximumLength(255).WithName(UomProperty.REF_NAME).WithMessage(ValidationMessage.MAX_LENGTH)
                .SetValidator(new MatchRegex(RegexConfig.GENERAL_RULE, RegexConfig.GENERAL_DESCRIPTION, systemContext, true));

            RuleFor(x => x.RefFactor)
                .NotEmpty().WithName(UomProperty.REF_FACTOR).WithMessage(ValidationMessage.REQUIRED)
                .When(x => !string.IsNullOrEmpty(x.RefName), ApplyConditionTo.CurrentValidator)
                    .Must(factor => factor.HasValue && !double.IsInfinity(factor.Value)).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE)
                    .LessThanOrEqualTo(MaxFactor).WithName(UomProperty.REF_FACTOR).WithMessage(ValidationMessage.GREATER_THAN_MAX_VALUE)
                    .GreaterThanOrEqualTo(MinFactor).WithName(UomProperty.REF_FACTOR).WithMessage(ValidationMessage.LESS_THAN_MIN_VALUE)
                .When(x => x.RefFactor != null, ApplyConditionTo.CurrentValidator)
                    .Equal(1).WithMessage(ValidationMessage.EQUAL_COMPARISON)
                .When(x => string.IsNullOrEmpty(x.RefName) && x.RefFactor != null, ApplyConditionTo.CurrentValidator);

            RuleFor(x => x.RefOffset)
                .NotEmpty().WithName(UomProperty.REF_OFFSET).WithMessage(ValidationMessage.REQUIRED)
                .When(x => !string.IsNullOrEmpty(x.RefName), ApplyConditionTo.CurrentValidator)
                    .Must(offset => offset.HasValue &&  !double.IsInfinity(offset.Value)).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE)
                    .LessThanOrEqualTo(MaxOffset).WithName(UomProperty.REF_OFFSET).WithMessage(ValidationMessage.GREATER_THAN_MAX_VALUE)
                    .GreaterThanOrEqualTo(MinOffset).WithName(UomProperty.REF_OFFSET).WithMessage(ValidationMessage.LESS_THAN_MIN_VALUE)
                .When(x => x.RefOffset != null, ApplyConditionTo.CurrentValidator)
                    .Equal(0).WithMessage(ValidationMessage.EQUAL_COMPARISON)
                .When(x => string.IsNullOrEmpty(x.RefName) && x.RefOffset != null, ApplyConditionTo.CurrentValidator);

            //RuleFor(x => x.CanonicalFactor)
            //    .NotNull().NotEmpty().WithMessage("Canonical Factor required").When(x => !string.IsNullOrEmpty(x.RefName));
            RuleFor(x => x.CanonicalFactor)
                .Must(factor => factor.HasValue && !double.IsInfinity(factor.Value)).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE)
                .LessThanOrEqualTo(MaxFactor).WithName(UomProperty.CANONICAL_FACTOR).WithMessage(ValidationMessage.GREATER_THAN_MAX_VALUE)
                .GreaterThanOrEqualTo(MinFactor).WithName(UomProperty.CANONICAL_FACTOR).WithMessage(ValidationMessage.LESS_THAN_MIN_VALUE)
                    .When(x => x.CanonicalFactor != null, ApplyConditionTo.CurrentValidator)
                .Equal(1).WithMessage(ValidationMessage.EQUAL_COMPARISON)
                    .When(x => string.IsNullOrEmpty(x.RefName) && x.CanonicalFactor != null, ApplyConditionTo.CurrentValidator);

            //RuleFor(x => x.CanonicalOffset)
            //    .NotNull().NotEmpty().WithMessage("Canonical Offset required").When(x => !string.IsNullOrEmpty(x.RefName));
            RuleFor(x => x.CanonicalOffset)
                .Must(offset => offset.HasValue &&  !double.IsInfinity(offset.Value)).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE)
                .LessThanOrEqualTo(MaxOffset).WithName(UomProperty.CANONICAL_OFFSET).WithMessage(ValidationMessage.GREATER_THAN_MAX_VALUE)
                .GreaterThanOrEqualTo(MinOffset).WithName(UomProperty.CANONICAL_OFFSET).WithMessage(ValidationMessage.LESS_THAN_MIN_VALUE)
                    .When(x => x.CanonicalOffset != null, ApplyConditionTo.CurrentValidator)
                .Equal(0).WithMessage(ValidationMessage.EQUAL_COMPARISON)
                    .When(x => string.IsNullOrEmpty(x.RefName) && x.CanonicalOffset != null, ApplyConditionTo.CurrentValidator);
        }
    }
}
