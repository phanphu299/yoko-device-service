using FluentValidation;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Repository;
using AssetTemplateProperty = AHI.Device.Function.Constant.ErrorMessage.ErrorProperty.AssetTemplate;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;

namespace AHI.Device.Function.Model.ImportModel.Validation
{
    public class AssetTemplateValidation : AbstractValidator<AssetTemplate>
    {
        public AssetTemplateValidation(ISystemContext systemContext)
        {
            RuleFor(x => x.Name).Cascade(CascadeMode.StopOnFirstFailure)
                                .Must(name => !string.IsNullOrWhiteSpace(name)).WithName(AssetTemplateProperty.NAME).WithMessage(ValidationMessage.REQUIRED)
                                .MaximumLength(255).WithMessage(ValidationMessage.MAX_LENGTH)
                                .SetValidator(new MatchRegex(RegexConfig.NAME_RULE, RegexConfig.NAME_DESCRIPTION, systemContext));
        }
    }
}
