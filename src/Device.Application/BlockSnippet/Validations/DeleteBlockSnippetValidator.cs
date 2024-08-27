using Device.Application.BlockSnippet.Command;
using FluentValidation;

namespace Device.Application.BlockSnippet.Validation
{
    public class DeleteBlockSnippetValidator : AbstractValidator<DeleteBlockSnippet>
    {
        public DeleteBlockSnippetValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
