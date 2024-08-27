using Device.Application.BlockSnippet.Command;
using FluentValidation;

namespace Device.Application.BlockSnippet.Validation
{
    public class AddBlockSnippetValidator : AbstractValidator<AddBlockSnippet>
    {
        public AddBlockSnippetValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
            RuleFor(x => x.TemplateCode).NotEmpty();
        }
    }
}
