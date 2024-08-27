using System.Linq;
using FluentValidation;
using FluentValidation.Validators;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Repository;
using DeviceTemplateProperty = AHI.Device.Function.Constant.ErrorMessage.ErrorProperty.DeviceTemplate;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;

namespace AHI.Device.Function.Model.ImportModel.Validation
{
    public class DeviceTemplateValidation : AbstractValidator<DeviceTemplate>
    {
        public DeviceTemplateValidation(IValidator<TemplatePayload> payloadValidator, IValidator<TemplateBinding> bindingValidator, ISystemContext systemContext)
        {
            RuleFor(x => x.Name).Cascade(CascadeMode.StopOnFirstFailure)
                                .Must(name => !string.IsNullOrWhiteSpace(name)).WithName(DeviceTemplateProperty.NAME).WithMessage(ValidationMessage.REQUIRED)
                                .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                .SetValidator(new MatchRegex(RegexConfig.NAME_RULE, RegexConfig.NAME_DESCRIPTION, systemContext))
                                .WithName(DeviceTemplateProperty.NAME).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE);
            RuleFor(x => x.Payloads).Cascade(CascadeMode.StopOnFirstFailure)
                                    .NotEmpty().WithName(DeviceTemplateProperty.PAYLOAD)
                                    .Must(payloads => payloads.All(payload => !string.IsNullOrEmpty(payload.JsonPayload)))
                                    .WithMessage(ValidationMessage.REQUIRED)
                                    .DependentRules(() =>
                                    {
                                        RuleFor(x => x).SetValidator(new DeviceTemplateSummaryValidator());
                                        RuleForEach(x => x.Payloads).SetValidator(payloadValidator);
                                    });

            RuleForEach(x => x.Bindings).SetValidator(bindingValidator).When(x => x.Bindings != null && x.Bindings.Any());
        }

        private class DeviceTemplateSummaryValidator : PropertyValidator
        {
            private readonly SummaryMessageSource _source;
            public DeviceTemplateSummaryValidator() : base(new SummaryMessageSource())
            {
                _source = Options.ErrorMessageSource as SummaryMessageSource;
            }

            public override bool ShouldValidateAsync(ValidationContext context)
            {
                return false;
            }

            protected override void PrepareMessageFormatterForValidationError(PropertyValidatorContext context)
            {
                // Do nothing to override default message format. Already handle message directly in IsValid method.
            }

            protected override bool IsValid(PropertyValidatorContext context)
            {
                var value = context.PropertyValue as DeviceTemplate;
                if (value == null)
                    return false;
                if (!ValidateTemplatePayloads(value, out var result))
                {
                    _source.Message = result.Message;
                    context.MessageFormatter.PlaceholderValues[result.PlaceHolderKey] = result.KeyType;
                    return false;
                }
                return true;
            }

            private bool ValidateTemplatePayloads(DeviceTemplate template, out (string Message, string PlaceHolderKey, string KeyType) result)
            {
                var counts = new TypeCounts();
                counts = template.Payloads.Aggregate(counts, (seed, payload) =>
                {
                    payload.Details.Aggregate(seed, (seed, detail) =>
                    {
                        switch (detail.KeyType)
                        {
                            case TemplateKeyTypes.DEVICEID:
                                {
                                    seed.DeviceIdCount++;
                                    break;
                                }
                            case TemplateKeyTypes.TIMESTAMP:
                                {
                                    seed.TimestampCount++;
                                    break;
                                }
                            case TemplateKeyTypes.CALCULATION:
                            case TemplateKeyTypes.METRIC:
                                {
                                    if (detail.Enabled)
                                        seed.TotalMetrics++;
                                    break;
                                }
                            default:
                                break;
                        }
                        return seed;
                    });
                    return seed;
                });
                result = GetErrorMessage(counts);
                template.TotalMetric = counts.TotalMetrics;
                return counts.DeviceIdCount == 1 && counts.TimestampCount == 1;
            }
            private (string, string, string) GetErrorMessage(TypeCounts counts)
            {
                var messages = counts.DeviceIdCount < 1 ? (ValidationMessage.REQUIRED, "PropertyName", "DeviceId")
                                : counts.DeviceIdCount > 1 ? (ValidationMessage.KEYTYPE_ONLY_ONCE, "KeyType", "DeviceId")
                                : counts.TimestampCount < 1 ? (ValidationMessage.REQUIRED, "PropertyName", "Timestamp")
                                : counts.TimestampCount > 1 ? (ValidationMessage.KEYTYPE_ONLY_ONCE, "KeyType", "Timestamp")
                                : default;
                return messages;
            }

        }
    }

    class TypeCounts
    {
        public int DeviceIdCount = 0;
        public int TimestampCount = 0;
        public int TotalMetrics = 0;
    }

    class SummaryMessageSource : FluentValidation.Resources.IStringSource
    {
        public string Message { get; set; }

        public string ResourceName => nameof(SummaryMessageSource);

        public System.Type ResourceType => typeof(SummaryMessageSource);

        public string GetString(IValidationContext context)
        {
            return Message;
        }
    }
}
