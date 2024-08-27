using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using Device.Application.Constant;
using Device.Application.Constants;
using Device.Application.Device.Command;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using AHI.Infrastructure.Service.Tag.Model;

namespace Device.Application.Device.Validation
{
    public class AddDeviceValidation : AbstractValidator<AddDevice>
    {
        public AddDeviceValidation()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.Id).NotEmpty()
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                .MaximumLength(50)
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH);
            RuleFor(x => x.TemplateId).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.RetentionDays).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => new { x.TelemetryTopic, x.CommandTopic, x.HasCommand, x.DeviceContent })
                .Must(c => IsValidTopicName(c.TelemetryTopic, c.CommandTopic, c.HasCommand, c.DeviceContent))
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);

            RuleForEach(x => x.Tags).SetValidator(
            new InlineValidator<UpsertTag> {
                    agValidator => agValidator.RuleFor(x => x.Key)
                                              .NotEmpty()
                                              .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                                              .MaximumLength(216)
                                              .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH)
                                              .Must(ContainsInvalidChar)
                                              .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID)
            }
        );
        }

        public override Task<ValidationResult> ValidateAsync(ValidationContext<AddDevice> context, CancellationToken cancellation = new CancellationToken())
        {
            return base.ValidateAsync(context, cancellation);
        }

        public static bool IsValidTopicName(string telemetryTopic, string commandTopic, bool? hasCommand, string deviceContent)
        {
            if (!string.IsNullOrEmpty(deviceContent) && (deviceContent.Contains(BrokerConstants.EMQX_MQTT) || deviceContent.Contains(BrokerConstants.EMQX_COAP)))
            {
                var telemetryTopicRegex = new Regex(RegexConstants.PATTERN_TELEMETRY_TOPIC);
                var commandTopicRegex = new Regex(RegexConstants.PATTERN_COMMAND_TOPIC);
                var oldTelemetryTopic = "$ahi/telemetry";
                var oldCommandTopic = "$ahi/commands";

                if (string.IsNullOrEmpty(telemetryTopic)
                        || (!telemetryTopicRegex.IsMatch(telemetryTopic) && !string.Equals(telemetryTopic, oldTelemetryTopic, System.StringComparison.InvariantCultureIgnoreCase))
                        || Encoding.UTF8.GetBytes(telemetryTopic).Length > NumberConstains.MAX_LENGTH_BYTE)
                {
                    return false;
                }

                if (hasCommand == true)
                {
                    if (string.IsNullOrEmpty(commandTopic)
                        || (!commandTopicRegex.IsMatch(commandTopic) && !string.Equals(commandTopic, oldCommandTopic, System.StringComparison.InvariantCultureIgnoreCase))
                        || Encoding.UTF8.GetBytes(commandTopic).Length > NumberConstains.MAX_LENGTH_BYTE)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool ContainsInvalidChar(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return true;
            return !input.Contains(':') && !input.Contains(';') && !input.Contains(',');
        }
    }
}
