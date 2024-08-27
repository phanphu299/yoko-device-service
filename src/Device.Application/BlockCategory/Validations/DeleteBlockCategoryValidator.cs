using Device.Application.BlockFunctionCategory.Command;
using FluentValidation;

namespace Device.Application.BlockCategory.Validation
{
    public class DeleteBlockCategoryValidator : AbstractValidator<DeleteBlockCategory>
    {
        public DeleteBlockCategoryValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
