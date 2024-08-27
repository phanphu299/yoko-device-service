using Device.Application.BlockFunctionCategory.Command;
using FluentValidation;

namespace Device.Application.BlockSnippet.Validation
{
    public class GetBlockSnippetByIdValidator : AbstractValidator<GetBlockCategoryById>
    {
        public GetBlockSnippetByIdValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
