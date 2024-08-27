
using AHI.Infrastructure.Exception;
using Device.Application.TemplateDetail.Command;
using FluentValidation;

namespace Device.Application.TemplateDetail.Validation
{
    public class AddTemplateDetailsValidation : AbstractValidator<AddTemplateDetails>
    {
        public AddTemplateDetailsValidation()
        {
            RuleFor(x => x.Key).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.Name).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.DataType).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.KeyTypeId).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            // RuleFor(x => x.Enabled).Must(x => x == false || x == true).WithMessage("Enabled is required").WithErrorCode(ErrorCodeConstants.TEMPLATE_DETAIL_ENABLED_REQUIRED.ToString("D"));
        }
    }
}
