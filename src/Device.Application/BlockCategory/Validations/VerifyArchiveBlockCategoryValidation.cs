using AHI.Infrastructure.Exception;
using Device.Application.BlockFunctionCategory.Model;
using FluentValidation;

namespace Device.Application.BlockCategory.Validation
{
    public class VerifyArchiveBlockCategoryValidation : AbstractValidator<ArchiveBlockCategoryDto>
    {
        public VerifyArchiveBlockCategoryValidation()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                    .MaximumLength(255).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_MAX_LENGTH);
        }
    }
}
