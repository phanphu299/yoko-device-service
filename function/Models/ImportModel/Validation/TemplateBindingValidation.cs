using System;
using FluentValidation;
using FluentValidation.Validators;
using Function.Extension;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Repository;
using DeviceTemplateProperty = AHI.Device.Function.Constant.ErrorMessage.ErrorProperty.DeviceTemplate;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;

namespace AHI.Device.Function.Model.ImportModel.Validation
{
    public class TemplateBindingValidation : AbstractValidator<TemplateBinding>
    {
        public TemplateBindingValidation(ISystemContext systemContext)
        {
            RuleFor(x => x.Key).Cascade(CascadeMode.StopOnFirstFailure)
                                .Must(key => !string.IsNullOrWhiteSpace(key)).WithName(DeviceTemplateProperty.KEY).WithMessage(ValidationMessage.REQUIRED)
                                .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                .SetValidator(new MatchRegex(RegexConfig.METRIC_RULE, RegexConfig.METRIC_DESCRIPTION, systemContext)).WithName(DeviceTemplateProperty.KEY).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE);

            RuleFor(x => x.DataType).Cascade(CascadeMode.StopOnFirstFailure)
                                    .NotEmpty().WithName(DeviceTemplateProperty.DATA_TYPE).WithMessage(ValidationMessage.REQUIRED)
                                    .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                    .Must((_, datatype, context) =>
                                    {
                                        context.MessageFormatter.PlaceholderValues["DeviceIdType"] = DataTypeConstants.TYPE_DEVICEID;
                                        context.MessageFormatter.PlaceholderValues["TimestampType"] = DataTypeConstants.TYPE_TIMESTAMP;
                                        return datatype != DataTypeConstants.TYPE_DEVICEID && datatype != DataTypeConstants.TYPE_TIMESTAMP;
                                    })
                                    .WithMessage(ValidationMessage.BINDING_TYPE_INVALID)
                                    .SetValidator(new MatchRegex(RegexConfig.GENERAL_RULE, RegexConfig.GENERAL_DESCRIPTION, systemContext));

            RuleFor(x => x.DefaultValue).Cascade(CascadeMode.StopOnFirstFailure)
                                        .NotNull().WithName(DeviceTemplateProperty.DEFAULT_VALUE).WithMessage(ValidationMessage.REQUIRED)
                                        .SetValidator(new DefaultValueValidator())
                                        .DependentRules(() =>
                                        {
                                            RuleFor(x => (string)x.DefaultValue).Cascade(CascadeMode.StopOnFirstFailure)
                                                                                .NotEmpty().WithName(DeviceTemplateProperty.DEFAULT_VALUE).WithMessage(ValidationMessage.REQUIRED)
                                                                                .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                                                                .SetValidator(new MatchRegex(RegexConfig.GENERAL_RULE, RegexConfig.GENERAL_DESCRIPTION, systemContext))
                                                                                .When(x => x.DefaultValue is string && x.DataType.Equals(DataTypeConstants.TYPE_TEXT, System.StringComparison.InvariantCultureIgnoreCase));
                                        });
        }

        private class DefaultValueValidator : PropertyValidator
        {
            private readonly DefaultValueMessageSource _source;
            public DefaultValueValidator() : base(new DefaultValueMessageSource())
            {
                _source = Options.ErrorMessageSource as DefaultValueMessageSource;
            }

            public override bool ShouldValidateAsync(ValidationContext context) => false;

            protected override void PrepareMessageFormatterForValidationError(PropertyValidatorContext context)
            {
                context.MessageFormatter.PlaceholderValues["PropertyName"] = context.PropertyName;
                context.MessageFormatter.PlaceholderValues["PropertyValue"] = context.PropertyValue;
            }

            protected override bool IsValid(PropertyValidatorContext context)
            {
                var instance = context.Instance as TemplateBinding;
                // empty datatype should already be validated by other rulefor
                if (string.IsNullOrEmpty(instance.DataType))
                    return true;

                var type = instance.DataType.ToLowerInvariant();
                var value = context.PropertyValue;

                try
                {
                    value.ValidateType(type);
                    return true;
                }
                catch (FormatException)
                {
                    _source.Message = ValidationMessage.GENERAL_INVALID_VALUE;
                    return false;
                }
                catch (OverflowException)
                {
                    _source.Message = ValidationMessage.GENERAL_OUT_OF_RANGE;
                    context.MessageFormatter.PlaceholderValues["PropertyType"] = type;
                    return false;
                }
            }
        }
    }

    class DefaultValueMessageSource : FluentValidation.Resources.IStringSource
    {
        public string Message { get; set; }

        public string ResourceName => nameof(DefaultValueMessageSource);

        public Type ResourceType => typeof(DefaultValueMessageSource);

        public string GetString(IValidationContext context)
        {
            return Message;
        }
    }
}
