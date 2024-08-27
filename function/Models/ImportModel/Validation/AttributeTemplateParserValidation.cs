using AHI.Device.Function.Constant;
using AHI.Device.Function.Model.ImportModel;
using AHI.Device.Function.Model.ImportModel.Attribute;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Repository;
using FluentValidation;
using AssetTemplateProperty = AHI.Device.Function.Constant.ErrorMessage.ErrorProperty.AttributeTemplate;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.ParseValidation;
namespace Function.Models.ImportModel.Validation
{
    public class AttributeTemplateParserValidation : AbstractValidator<AttributeTemplate>
    {
        public AttributeTemplateParserValidation(ISystemContext systemContext)
        {

            RuleFor(x => x.AttributeName).Cascade(CascadeMode.StopOnFirstFailure)
                                         .Must(name => !string.IsNullOrWhiteSpace(name)).WithName(AssetTemplateProperty.ATTRIBUTE_NAME).WithMessage(ValidationMessage.PARSER_MANDATORY_FIELDS_REQUIRED)
                                         .MaximumLength(255).WithMessage(ValidationMessage.PARSER_INVALID_DATA)
                                         .SetValidator(new MatchRegex(RegexConfig.ASSET_ATTRIBUTE_RULE, RegexConfig.ASSET_ATTRIBUTE_DESCRIPTION, systemContext)).WithName(AssetTemplateProperty.ATTRIBUTE_NAME).WithMessage(ValidationMessage.PARSER_INVALID_DATA);

            RuleFor(x => x.AttributeType).Cascade(CascadeMode.StopOnFirstFailure)
                             .Must(type => !string.IsNullOrWhiteSpace(type)).WithName(AssetTemplateProperty.TYPE).WithMessage(ValidationMessage.PARSER_MANDATORY_FIELDS_REQUIRED)
                             .MaximumLength(255).WithMessage(ValidationMessage.PARSER_INVALID_DATA)
                             .SetValidator(new MatchRegex(RegexConfig.ASSET_ATTRIBUTE_RULE, RegexConfig.ASSET_ATTRIBUTE_DESCRIPTION, systemContext)).WithName(AssetTemplateProperty.TYPE).WithMessage(ValidationMessage.PARSER_INVALID_DATA);

            When(x => x.Type == AssetAttributeType.STATIC, () =>
            {
                RuleFor(x => x.DataType).Cascade(CascadeMode.StopOnFirstFailure)
                            .Must(dataType => !string.IsNullOrWhiteSpace(dataType)).WithName(AssetTemplateProperty.DATA_TYPE).WithMessage(ValidationMessage.PARSER_MANDATORY_FIELDS_REQUIRED)
                            .MaximumLength(255).WithMessage(ValidationMessage.PARSER_INVALID_DATA)
                            .SetValidator(new MatchRegex(RegexConfig.ASSET_ATTRIBUTE_RULE, RegexConfig.ASSET_ATTRIBUTE_DESCRIPTION, systemContext)).WithName(AssetTemplateProperty.DATA_TYPE).WithMessage(ValidationMessage.PARSER_INVALID_DATA);
            });
        }
    }
}