using AHI.Infrastructure.Exception;
using Device.Application.TemplatePayload.Command;
using FluentValidation;

namespace Device.Application.TemplatePayload.Validation
{
    public class AddTemplatePayloadValidation : AbstractValidator<AddTemplatePayload>
    {
        public AddTemplatePayloadValidation()
        {
            RuleFor(x => x.JsonPayload).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
        }
    }
}
