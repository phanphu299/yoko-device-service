using System;
using System.Collections.Generic;
using AHI.Infrastructure.Exception;
using Device.Application.Device.Command.Model;
using FluentValidation;
using Newtonsoft.Json;

namespace Device.Application.Device.Validation
{
    public class VerifyArchiveDeviceValidation : AbstractValidator<ArchiveDeviceDto>
    {

        public VerifyArchiveDeviceValidation()
        {
            RuleFor(x => x.Id)
                    .NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                    .MaximumLength(50).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH);
            RuleFor(x => x.Name).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.Status).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.TemplateId).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                    .Must(x => x != System.Guid.Empty).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
            RuleFor(x => x.RetentionDays).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.DeviceContent)
                    .Must(x =>
                    {
                        if (x == null)
                            return true;

                        var deviceContent = JsonConvert.DeserializeObject<Dictionary<string, object>>(x);
                        // a valid deviceContent may contain a brokerId
                        // if there is, it must be an null, empty, white space string or GUID string
                        if (deviceContent.TryGetValue("brokerId", out var brokerId))
                        {
                            if (brokerId != null
                                && !string.IsNullOrWhiteSpace(brokerId.ToString())
                                && !Guid.TryParse(brokerId.ToString(), out _))
                                return false;
                        }
                        return true;
                    }).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
        }
    }
}
