using Device.Application.BlockSnippet.Command;
using FluentValidation;

namespace Device.Application.BlockSnippet.Validation
{
    public class UpdateBlockSnippetValidator : AbstractValidator<UpdateBlockSnippet>
    {
        public UpdateBlockSnippetValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
            RuleFor(x => x.TemplateCode).NotEmpty();
        }
    }
}
