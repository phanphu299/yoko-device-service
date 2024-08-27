using Device.Application.Asset.Command;
using FluentValidation;
using AHI.Infrastructure.Exception;

namespace Device.Application.Asset.Validation
{
    public class DeleteListTableValidation : AbstractValidator<DeleteListTable>
    {
        public DeleteListTableValidation()
        {
            RuleFor(x => x.Ids).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
        }
    }
}
