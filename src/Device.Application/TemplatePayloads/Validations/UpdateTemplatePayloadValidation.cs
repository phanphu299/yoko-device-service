using AHI.Infrastructure.Exception;
using Device.Application.TemplatePayload.Command;
using FluentValidation;

namespace Device.Application.TemplatePayload.Validation
{
    public class UpdateTemplatePayloadValidation : AbstractValidator<UpdateTemplatePayload>
    {
        public UpdateTemplatePayloadValidation()
        {
            RuleFor(x => x.JsonPayload).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
        }
    }
}
