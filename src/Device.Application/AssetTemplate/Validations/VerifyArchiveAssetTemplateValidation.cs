using System;
using AHI.Infrastructure.Exception;
using Device.Application.AssetAttributeTemplate.Command.Model;
using Device.Application.AssetTemplate.Command.Model;
using Device.Application.Constant;
using Device.ApplicationExtension.Extension;
using FluentValidation;
using static Device.Application.Constant.AttributeTypeConstants;

namespace Device.Application.AssetTemplate.Validation
{
    public class VerifyArchiveAssetTemplateValidation : AbstractValidator<ArchiveAssetTemplateDto>
    {
        public VerifyArchiveAssetTemplateValidation(IAttributeTemplateValidation attributeTemplateValidator)
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleForEach(a => a.Attributes)
                .Must(attributeTemplateValidator.Validate).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID)
                .ChildRules(attribute =>
                {
                    attribute.RuleFor(x => x.Id).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                    attribute.RuleFor(x => x.AssetTemplateId).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                    attribute.RuleFor(x => x.Name).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                    attribute.RuleFor(x => x.AttributeType).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                    attribute.RuleFor(x => x.DataType).NotEmpty().When(x => x.AttributeType != AttributeTypeConstants.TYPE_ALIAS).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                })
                .When(a => a.Attributes != null);
        }
    }

    public interface IAttributeTemplateValidation
    {
        bool Validate(GetAssetAttributeTemplateDto dto);
    }

    public abstract class BaseAttributeTemplateValidation : IAttributeTemplateValidation
    {
        private IAttributeTemplateValidation _next;

        public void SetNextValidation(IAttributeTemplateValidation next)
        {
            _next = next;
        }

        public bool Validate(GetAssetAttributeTemplateDto dto)
        {
            if (CanApply(dto.AttributeType))
            {
                return ValidateAttributeTemplate(dto);
            }
            if (_next != null)
            {
                return _next.Validate(dto);
            }
            return false;
        }

        protected abstract bool CanApply(string type);
        protected abstract bool ValidateAttributeTemplate(GetAssetAttributeTemplateDto dto);
    }

    public class StaticAttributeTemplateValidation : BaseAttributeTemplateValidation
    {
        protected override bool CanApply(string type) => type == TYPE_STATIC;

        protected override bool ValidateAttributeTemplate(GetAssetAttributeTemplateDto dto)
        {
            return dto.Payload == null;
        }
    }

    public class DynamicAttributeTemplateValidation : BaseAttributeTemplateValidation
    {
        protected override bool CanApply(string type) => type == TYPE_DYNAMIC;

        protected override bool ValidateAttributeTemplate(GetAssetAttributeTemplateDto dto)
        {
            if (dto.Payload == null)
                return false;

            return dto.Payload.GetId() != Guid.Empty
                && !string.IsNullOrWhiteSpace(dto.Payload.GetMarkupName())
                && !string.IsNullOrWhiteSpace(dto.Payload.MetricKey);
        }
    }

    public class IntegrationAttributeTemplateValidation : BaseAttributeTemplateValidation
    {
        protected override bool CanApply(string type) => type == TYPE_INTEGRATION;

        protected override bool ValidateAttributeTemplate(GetAssetAttributeTemplateDto dto)
        {
            if (dto.Payload == null)
                return true;

            return dto.Payload.GetId() != Guid.Empty
                && !string.IsNullOrWhiteSpace(dto.Payload.GetIntegrationMarkupName())
                && !string.IsNullOrWhiteSpace(dto.Payload.GetDeviceMarkupName())
                && !string.IsNullOrWhiteSpace(dto.Payload.MetricKey);
        }
    }

    public class CommandAttributeTemplateValidation : BaseAttributeTemplateValidation
    {
        protected override bool CanApply(string type) => type == TYPE_COMMAND;

        protected override bool ValidateAttributeTemplate(GetAssetAttributeTemplateDto dto)
        {
            if (dto.Payload == null)
                return true;

            return dto.Payload.GetId() != Guid.Empty
                && dto.Payload.GetDeviceTemplateId() != Guid.Empty
                && !string.IsNullOrWhiteSpace(dto.Payload.GetMarkupName())
                && !string.IsNullOrWhiteSpace(dto.Payload.MetricKey);
        }
    }

    public class AliasAttributeTemplateValidation : BaseAttributeTemplateValidation
    {
        protected override bool CanApply(string type) => type == TYPE_ALIAS;

        protected override bool ValidateAttributeTemplate(GetAssetAttributeTemplateDto dto)
        {
            return true;
        }
    }

    public class RuntimeAttributeTemplateValidation : BaseAttributeTemplateValidation
    {
        protected override bool CanApply(string type) => type == TYPE_RUNTIME;

        protected override bool ValidateAttributeTemplate(GetAssetAttributeTemplateDto dto)
        {
            if (dto.Payload == null)
                return true;

            return dto.Payload.GetId() != Guid.Empty;
        }
    }
}
