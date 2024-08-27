using AHI.Infrastructure.Exception;
using Device.Job.Model;
using FluentValidation;

namespace Device.Job.Validation
{
    public class AddJobValidation : AbstractValidator<AddJob>
    {
        public AddJobValidation()
        {
            RuleFor(x => x.Type).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.OutputType).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.FolderPath).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.Payload).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
        }
    }
}