using System;
using System.Text;
using System.Text.RegularExpressions;
using AHI.Infrastructure.Exception;
using Device.Application.Constant;
using Device.Application.Constants;
using Device.Application.Device.Command;
using FluentValidation;
using AHI.Infrastructure.Service.Tag.Model;

namespace Device.Application.Device.Validation
{
    public class UpdateDeviceValidation : AbstractValidator<UpdateDevice>
    {
        public UpdateDeviceValidation(IServiceProvider serviceProvider)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.Id).NotEmpty()
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                .MaximumLength(50)
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH);
            RuleFor(x => new { x.TelemetryTopic, x.CommandTopic, x.HasCommand })
               .Must(c => IsValidTopicName(c.TelemetryTopic, c.CommandTopic, c.HasCommand))
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

        private bool IsValidTopicName(string telemetryTopic, string commandTopic, bool? hasCommand)
        {
            var telemetryTopicRegex = new Regex(RegexConstants.PATTERN_TELEMETRY_TOPIC);
            var commandTopicRegex = new Regex(RegexConstants.PATTERN_COMMAND_TOPIC);
            var telemetryTopicOldData = "$ahi/telemetry";

            if (!string.IsNullOrEmpty(telemetryTopic) && !telemetryTopic.Equals(telemetryTopicOldData))
            {
                if (!telemetryTopicRegex.IsMatch(telemetryTopic))
                {
                    return false;
                }

                if (Encoding.UTF8.GetBytes(telemetryTopic).Length > NumberConstains.MAX_LENGTH_BYTE)
                {
                    return false;
                }
            }

            if (hasCommand == true)
            {
                if (string.IsNullOrEmpty(commandTopic) || !commandTopicRegex.IsMatch(commandTopic))
                {
                    return false;
                }

                if (Encoding.UTF8.GetBytes(commandTopic).Length > NumberConstains.MAX_LENGTH_BYTE)
                {
                    return false;
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
