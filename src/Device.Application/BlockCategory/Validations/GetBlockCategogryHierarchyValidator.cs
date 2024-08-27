using Device.Application.BlockCategory.Command;
using FluentValidation;

namespace Device.Application.BlockCategory.Validation
{
    public class GetBlockCategoryHierarchyValidator : AbstractValidator<GetBlockCategoryHierarchy>
    {
        public GetBlockCategoryHierarchyValidator()
        {
            RuleFor(x => x.Name).MaximumLength(255);
        }
    }
}
