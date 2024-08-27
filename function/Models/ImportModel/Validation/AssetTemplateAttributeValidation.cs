using FluentValidation;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Repository;
using AssetTemplateProperty = AHI.Device.Function.Constant.ErrorMessage.ErrorProperty.AssetTemplate;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;

namespace AHI.Device.Function.Model.ImportModel.Validation
{
    public class AssetTemplateAttributeValidation : AbstractValidator<AssetTemplateAttribute>
    {
        public AssetTemplateAttributeValidation(ISystemContext systemContext)
        {
            RuleFor(x => x.AttributeName).Cascade(CascadeMode.StopOnFirstFailure)
                                         .Must(name => !string.IsNullOrWhiteSpace(name)).WithName(AssetTemplateProperty.ATTRIBUTE_NAME).WithMessage(ValidationMessage.REQUIRED)
                                         .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                         .SetValidator(new MatchRegex(RegexConfig.ASSET_ATTRIBUTE_RULE, RegexConfig.ASSET_ATTRIBUTE_DESCRIPTION, systemContext)).WithName(AssetTemplateProperty.ATTRIBUTE_NAME).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE);

            RuleFor(x => x.DeviceTemplate).Cascade(CascadeMode.StopOnFirstFailure)
                                          .Must(template => !string.IsNullOrWhiteSpace(template)).WithName(AssetTemplateProperty.DEVICE_TEMPLATE).WithMessage(ValidationMessage.REQUIRED)
                                          .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                          .SetValidator(new MatchRegex(RegexConfig.GENERAL_RULE, RegexConfig.GENERAL_DESCRIPTION, systemContext)).WithName(AssetTemplateProperty.DEVICE_TEMPLATE).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE)
                                          .When(x => x.Type == AssetAttributeType.DYNAMIC);

            When(x => x.Type == AssetAttributeType.INTEGRATION, () =>
            {
                RuleFor(x => x.Channel).Cascade(CascadeMode.StopOnFirstFailure)
                                       .Must(channel => !string.IsNullOrWhiteSpace(channel)).WithName(AssetTemplateProperty.CHANNEL).WithMessage(ValidationMessage.REQUIRED)
                                       .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                       .SetValidator(new MatchRegex(RegexConfig.GENERAL_RULE, RegexConfig.GENERAL_DESCRIPTION, systemContext)).WithName(AssetTemplateProperty.CHANNEL).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE);

                RuleFor(x => x.ChannelMarkup).Cascade(CascadeMode.StopOnFirstFailure)
                                             .Must(channel => !string.IsNullOrWhiteSpace(channel)).WithName(AssetTemplateProperty.MARKUP_CHANNEL).WithMessage(ValidationMessage.REQUIRED)
                                             .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                             .SetValidator(new MatchRegex(RegexConfig.GENERAL_RULE, RegexConfig.GENERAL_DESCRIPTION, systemContext)).WithName(AssetTemplateProperty.MARKUP_CHANNEL).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE);

                RuleFor(x => x.Device).Cascade(CascadeMode.StopOnFirstFailure)
                                      .Must(channel => !string.IsNullOrWhiteSpace(channel)).WithName(AssetTemplateProperty.DEVICE).WithMessage(ValidationMessage.REQUIRED)
                                      .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                      .SetValidator(new MatchRegex(RegexConfig.DEVICE_RULE, RegexConfig.DEVICE_RULE_DESCRIPTION, systemContext)).WithName(AssetTemplateProperty.DEVICE).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE);

                RuleFor(x => x.DataType).Cascade(CascadeMode.StopOnFirstFailure)
                                        .Must(datatype => !string.IsNullOrWhiteSpace(datatype)).WithName(AssetTemplateProperty.DATA_TYPE).WithMessage(ValidationMessage.REQUIRED)
                                        .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                        .SetValidator(new MatchRegex(RegexConfig.GENERAL_RULE, RegexConfig.GENERAL_DESCRIPTION, systemContext)).WithName(AssetTemplateProperty.DATA_TYPE).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE);
            });

            When(x => x.Type == AssetAttributeType.DYNAMIC || x.Type == AssetAttributeType.INTEGRATION, () =>
            {
                RuleFor(x => x.DeviceMarkup).Cascade(CascadeMode.StopOnFirstFailure)
                                            .Must(device => !string.IsNullOrWhiteSpace(device)).WithName(AssetTemplateProperty.MARKUP_DEVICE).WithMessage(ValidationMessage.REQUIRED)
                                            .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                            .SetValidator(new MatchRegex(RegexConfig.GENERAL_RULE, RegexConfig.GENERAL_DESCRIPTION, systemContext)).WithName(AssetTemplateProperty.MARKUP_DEVICE).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE);

                RuleFor(x => x.Metric).Cascade(CascadeMode.StopOnFirstFailure)
                                      .Must(metric => !string.IsNullOrWhiteSpace(metric)).WithName(AssetTemplateProperty.METRIC).WithMessage(ValidationMessage.REQUIRED)
                                      .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                      .SetValidator(new MatchRegex(RegexConfig.METRIC_RULE, RegexConfig.METRIC_DESCRIPTION, systemContext)).WithName(AssetTemplateProperty.METRIC).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE);
            });

            Unless(x => x.Type == AssetAttributeType.RUNTIME, () =>
            {
                RuleFor(x => x.Uom).Cascade(CascadeMode.StopOnFirstFailure)
                                   .MaximumLength(255).WithName(AssetTemplateProperty.UOM).WithMessage(ValidationMessage.MAX_LENGTH)
                                   .SetValidator(new MatchRegex(RegexConfig.GENERAL_RULE, RegexConfig.GENERAL_DESCRIPTION, systemContext, true)).WithName(AssetTemplateProperty.UOM).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE)
                                   .When(x => !string.IsNullOrEmpty(x.Uom));
            });
        }
    }
}
