using FluentValidation;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Repository;
using DeviceProperty = AHI.Device.Function.Constant.ErrorMessage.ErrorProperty.Device;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;

namespace AHI.Device.Function.Model.ImportModel.Validation
{
    public class DeviceValidation : AbstractValidator<DeviceModel>
    {
        public DeviceValidation(ISystemContext systemContext)
        {
            RuleFor(x => x.Id).Cascade(CascadeMode.StopOnFirstFailure)
                              .Must(id => !string.IsNullOrWhiteSpace(id)).WithName(DeviceProperty.ID).WithMessage(ValidationMessage.REQUIRED)
                              .MaximumLength(50).WithMessage(ValidationMessage.MAX_LENGTH)
                              .SetValidator(new MatchRegex(RegexConfig.DEVICE_RULE, RegexConfig.DEVICE_RULE_DESCRIPTION, systemContext));

            RuleFor(x => x.Name).Cascade(CascadeMode.StopOnFirstFailure)
                                .Must(name => !string.IsNullOrWhiteSpace(name)).WithName(DeviceProperty.NAME).WithMessage(ValidationMessage.REQUIRED)
                                .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                .SetValidator(new MatchRegex(RegexConfig.NAME_RULE, RegexConfig.NAME_DESCRIPTION, systemContext))
                                .WithName(DeviceProperty.NAME).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE);

            RuleFor(x => x.Template).Cascade(CascadeMode.StopOnFirstFailure)
                                    .Must(template => !string.IsNullOrWhiteSpace(template)).WithName(DeviceProperty.TEMPLATE).WithMessage(ValidationMessage.REQUIRED)
                                    .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                    .SetValidator(new MatchRegex(RegexConfig.GENERAL_RULE, RegexConfig.GENERAL_DESCRIPTION, systemContext));

            RuleFor(x => x.RetentionDays).GreaterThanOrEqualTo(1).WithName(DeviceProperty.RETENTION_DAYS).WithMessage(ValidationMessage.LESS_THAN_MIN_VALUE)
                                         .LessThanOrEqualTo(int.MaxValue).WithName(DeviceProperty.RETENTION_DAYS).WithMessage(ValidationMessage.GREATER_THAN_MAX_VALUE)
                                         .When(x => x.RetentionDays.HasValue);
        }
    }
}
