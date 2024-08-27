using FluentValidation;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Repository;
using DeviceTemplateProperty = AHI.Device.Function.Constant.ErrorMessage.ErrorProperty.DeviceTemplate;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;

namespace AHI.Device.Function.Model.ImportModel.Validation
{
    public class TemplateDetailValidation : AbstractValidator<TemplateDetail>
    {
        public TemplateDetailValidation(ISystemContext systemContext)
        {
            RuleFor(x => x.Key).Cascade(CascadeMode.StopOnFirstFailure)
                               .NotEmpty().WithName(DeviceTemplateProperty.KEY).WithMessage(ValidationMessage.REQUIRED)
                               .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                               .SetValidator(new MatchRegex(RegexConfig.METRIC_RULE, RegexConfig.METRIC_DESCRIPTION, systemContext)).WithName(DeviceTemplateProperty.KEY).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE);
            RuleFor(x => x.Name).Cascade(CascadeMode.StopOnFirstFailure)
                                .NotEmpty().WithName(DeviceTemplateProperty.NAME).WithMessage(ValidationMessage.REQUIRED)
                                .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                .SetValidator(new MatchRegex(RegexConfig.GENERAL_RULE, RegexConfig.NAME_DESCRIPTION, systemContext))
                                .WithName(DeviceTemplateProperty.NAME).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE);
            RuleFor(x => x.KeyType).Cascade(CascadeMode.StopOnFirstFailure)
                                   .NotEmpty().WithName(DeviceTemplateProperty.KEY_TYPE).WithMessage(ValidationMessage.REQUIRED)
                                   .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                   .SetValidator(new MatchRegex(RegexConfig.GENERAL_RULE, RegexConfig.GENERAL_DESCRIPTION, systemContext))
                                   .DependentRules(() =>
                                   {
                                       RuleFor(x => x.Expression).Cascade(CascadeMode.StopOnFirstFailure)
                                                                 .NotEmpty().WithName(DeviceTemplateProperty.EXPRESSION).WithMessage(ValidationMessage.REQUIRED)
                                                                 .SetValidator(new MatchRegex(RegexConfig.EXPRESSION_RULE, RegexConfig.EXPRESSION_DESCRIPTION, systemContext))
                                                                 .When(x => x.KeyType == TemplateKeyTypes.CALCULATION);
                                       RuleFor(x => x.DataType).Cascade(CascadeMode.StopOnFirstFailure)
                                                               .Must((_, datatype, context) =>
                                                               {
                                                                   context.MessageFormatter.PlaceholderValues["KeyType"] = TemplateKeyTypes.DEVICEID;
                                                                   context.MessageFormatter.PlaceholderValues["ComparisonValue"] = DataTypeConstants.TYPE_DEVICEID;
                                                                   return datatype == DataTypeConstants.TYPE_DEVICEID;
                                                               }).WithName(DeviceTemplateProperty.DATA_TYPE)
                                                               .WithMessage(ValidationMessage.DETAIL_TYPE_INVALID)
                                                                   .When(x => x.KeyType == TemplateKeyTypes.DEVICEID, ApplyConditionTo.CurrentValidator)
                                                               .Must((_, datatype, context) =>
                                                               {
                                                                   context.MessageFormatter.PlaceholderValues["KeyType"] = TemplateKeyTypes.TIMESTAMP;
                                                                   context.MessageFormatter.PlaceholderValues["ComparisonValue"] = DataTypeConstants.TYPE_TIMESTAMP;
                                                                   return datatype == DataTypeConstants.TYPE_TIMESTAMP;
                                                               })
                                                               .WithMessage(ValidationMessage.DETAIL_TYPE_INVALID)
                                                                   .When(x => x.KeyType == TemplateKeyTypes.TIMESTAMP, ApplyConditionTo.CurrentValidator)
                                                               .Must((_, datatype, context) =>
                                                               {
                                                                   context.MessageFormatter.PlaceholderValues["MetricKeyType"] = TemplateKeyTypes.METRIC;
                                                                   context.MessageFormatter.PlaceholderValues["CalculationKeyType"] = TemplateKeyTypes.CALCULATION;
                                                                   context.MessageFormatter.PlaceholderValues["DeviceIdType"] = DataTypeConstants.TYPE_DEVICEID;
                                                                   context.MessageFormatter.PlaceholderValues["TimestampType"] = DataTypeConstants.TYPE_TIMESTAMP;
                                                                   return datatype != DataTypeConstants.TYPE_DEVICEID && datatype != DataTypeConstants.TYPE_TIMESTAMP;
                                                               })
                                                               .WithMessage(ValidationMessage.DETAIL_METRIC_TYPE_INVALID)
                                                                   .When(x => x.KeyType != TemplateKeyTypes.DEVICEID && x.KeyType != TemplateKeyTypes.TIMESTAMP, ApplyConditionTo.CurrentValidator);
                                   });
            RuleFor(x => x.DataType).Cascade(CascadeMode.StopOnFirstFailure)
                                .NotEmpty().WithName(DeviceTemplateProperty.DATA_TYPE).WithMessage(ValidationMessage.REQUIRED)
                                .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                .SetValidator(new MatchRegex(RegexConfig.GENERAL_RULE, RegexConfig.GENERAL_DESCRIPTION, systemContext));
        }
    }
}
