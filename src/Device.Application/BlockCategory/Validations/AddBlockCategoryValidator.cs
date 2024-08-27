using Device.Application.BlockFunctionCategory.Command;
using FluentValidation;

namespace Device.Application.BlockCategory.Validation
{
    public class AddBlockCategoryValidator : AbstractValidator<AddBlockCategory>
    {
        public AddBlockCategoryValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
