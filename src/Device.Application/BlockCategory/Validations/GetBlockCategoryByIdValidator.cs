using Device.Application.BlockFunctionCategory.Command;
using FluentValidation;

namespace Device.Application.BlockCategory.Validation
{
    public class GetBlockCategoryByIdValidator : AbstractValidator<DeleteBlockCategory>
    {
        public GetBlockCategoryByIdValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
