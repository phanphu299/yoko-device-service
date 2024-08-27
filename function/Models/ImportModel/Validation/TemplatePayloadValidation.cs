using FluentValidation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Repository;
using DisplayName = AHI.Device.Function.Constant.ErrorMessage.ErrorProperty.DeviceTemplate;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;
using AHI.Infrastructure.SharedKernel.Extension;

namespace AHI.Device.Function.Model.ImportModel.Validation
{
    public class TemplatePayloadValidation : AbstractValidator<TemplatePayload>
    {
        public TemplatePayloadValidation(IValidator<TemplateDetail> validator, ISystemContext systemContext)
        {
            RuleFor(x => x.JsonPayload)
                    .Must(payload => ValidateJson(payload)).WithName(DisplayName.PAYLOAD).WithMessage(ValidationMessage.PAYLOAD_INVALID)
                    .SetValidator(new MatchRegex(RegexConfig.PAYLOAD_RULE, RegexConfig.PAYLOAD_DESCRIPTION, systemContext)).WithName(DisplayName.PAYLOAD).WithMessage(ValidationMessage.GENERAL_INVALID_VALUE);
            RuleForEach(x => x.Details).SetValidator(validator);
        }

        private bool ValidateJson(string jsonString)
        {
            try
            {
                _ = jsonString.FromJson<JObject>();
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }
    }
}
