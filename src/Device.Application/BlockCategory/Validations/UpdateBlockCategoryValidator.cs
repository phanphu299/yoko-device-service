using Device.Application.BlockFunctionCategory.Command;
using FluentValidation;

namespace Device.Application.BlockCategory.Validation
{
    public class UpdateBlockCategoryValidator : AbstractValidator<UpdateBlockCategory>
    {
        public UpdateBlockCategoryValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
