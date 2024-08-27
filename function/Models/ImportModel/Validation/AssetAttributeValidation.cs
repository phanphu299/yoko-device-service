using FluentValidation;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Repository;
using AssetAttributeProperty = AHI.Device.Function.Constant.ErrorMessage.ErrorProperty.AssetAttribute;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.ParseValidation;

namespace AHI.Device.Function.Model.ImportModel.Validation
{
    public class AssetAttributeValidation : AbstractValidator<AssetAttribute>
    {
        public AssetAttributeValidation(ISystemContext systemContext)
        {
            RuleFor(x => x.AttributeName).Cascade(CascadeMode.StopOnFirstFailure)
                                         .Must(name => !string.IsNullOrWhiteSpace(name)).WithName(AssetAttributeProperty.ATTRIBUTE_NAME).WithMessage(ValidationMessage.PARSER_MANDATORY_FIELDS_REQUIRED)
                                         .MaximumLength(255).WithMessage(ValidationMessage.PARSER_INVALID_DATA)
                                         .SetValidator(new MatchRegex(RegexConfig.ASSET_ATTRIBUTE_RULE, RegexConfig.ASSET_ATTRIBUTE_DESCRIPTION, systemContext)).WithName(AssetAttributeProperty.ATTRIBUTE_NAME).WithMessage(ValidationMessage.PARSER_INVALID_DATA);

            RuleFor(x => x.AttributeType).Cascade(CascadeMode.StopOnFirstFailure)
                             .Must(type => !string.IsNullOrWhiteSpace(type)).WithName(AssetAttributeProperty.ATTRIBUTE_TYPE).WithMessage(ValidationMessage.PARSER_MANDATORY_FIELDS_REQUIRED);

            When(x => x.IsStaticAttribute || x.IsRuntimeAttribute || x.IsIntegrationAttribute, () =>
            {
                RuleFor(x => x.DataType).Cascade(CascadeMode.StopOnFirstFailure)
                                            .Must(dataType => !string.IsNullOrWhiteSpace(dataType)).WithName(AssetAttributeProperty.DATA_TYPE).WithMessage(ValidationMessage.PARSER_MANDATORY_FIELDS_REQUIRED);
            });
        }
    }
}
