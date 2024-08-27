using AHI.Infrastructure.Exception;
using Device.Application.Asset.Command.Model;
using FluentValidation;
using System;
using Device.ApplicationExtension.Extension;
using static Device.Application.Constant.AttributeTypeConstants;

namespace Device.Application.Asset.Validation
{
    public class VerifyArchiveAssetValidation : AbstractValidator<ArchiveAssetDto>
    {
        public VerifyArchiveAssetValidation(IAttributeValidation attributeValidator)
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
            RuleForEach(a => a.Triggers).Must(ValidateRuntimeTrigger).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID);
            RuleForEach(a => a.Attributes)
                .Must(attributeValidator.Validate).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_INVALID)
                .ChildRules(attribute =>
                {
                    attribute.RuleFor(x => x.Id).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                    attribute.RuleFor(x => x.AssetId).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                    attribute.RuleFor(x => x.Name).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                    attribute.RuleFor(x => x.AttributeType).NotEmpty().WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                    attribute.RuleFor(x => x.DataType).NotEmpty().When(x => x.AttributeType != TYPE_ALIAS).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED);
                })
                .When(a => a.Attributes != null);

            RuleForEach(a => a.Mappings)
                .Must(attributeValidator.Validate).WithMessage(ExceptionErrorCode.DetailCode.ERROR_VALIDATION_REQUIRED)
                .When(a => a.Mappings != null);
        }

        private bool ValidateRuntimeTrigger(AttributeMapping trigger)
        {
            return trigger.GetId() != Guid.Empty
                && trigger.GetAssetId() != Guid.Empty
                && trigger.GetAttributeId() != Guid.Empty
                && trigger.GetTriggerAssetId() != Guid.Empty
                && trigger.GetTriggerAttributeId() != Guid.Empty;
        }
    }

    public interface IAttributeValidation
    {
        bool Validate(AssetAttributeDto dto);
        bool Validate(AttributeMapping mapping);
    }

    public abstract class BaseAttributeValidation : IAttributeValidation
    {
        private IAttributeValidation _next;

        public void SetNextValidation(IAttributeValidation next)
        {
            _next = next;
        }

        public bool Validate(AssetAttributeDto dto)
        {
            if (CanApply(dto.AttributeType))
            {
                return ValidateAttribute(dto);
            }
            if (_next != null)
            {
                return _next.Validate(dto);
            }
            return false;
        }

        public bool Validate(AttributeMapping mapping)
        {
            if (CanApply(mapping.GetAttributeType()))
            {
                return ValidateMapping(mapping);
            }
            if (_next != null)
            {
                return _next.Validate(mapping);
            }
            return false;
        }

        protected abstract bool CanApply(string type);
        protected abstract bool ValidateAttribute(AssetAttributeDto dto);
        protected abstract bool ValidateMapping(AttributeMapping mapping);
    }

    public class StaticAttributeValidation : BaseAttributeValidation
    {
        protected override bool CanApply(string type) => type == TYPE_STATIC;

        protected override bool ValidateAttribute(AssetAttributeDto dto)
        {
            return dto.Payload == null;
        }

        protected override bool ValidateMapping(AttributeMapping mapping)
        {
            return mapping.GetId() != Guid.Empty
                && mapping.GetAssetId() != Guid.Empty
                && mapping.GetAssetAttributeTemplateId() != Guid.Empty;
        }
    }

    public class DynamicAttributeValidation : BaseAttributeValidation
    {
        protected override bool CanApply(string type) => type == TYPE_DYNAMIC;

        protected override bool ValidateAttribute(AssetAttributeDto dto)
        {
            return dto.Payload != null
                && dto.Payload.GetId() != Guid.Empty
                && !string.IsNullOrWhiteSpace(dto.Payload.MetricKey);
        }

        protected override bool ValidateMapping(AttributeMapping mapping)
        {
            return mapping.GetId() != Guid.Empty
                && mapping.GetAssetId() != Guid.Empty
                && mapping.GetAssetAttributeTemplateId() != Guid.Empty
                && !string.IsNullOrWhiteSpace(mapping.MetricKey);
        }
    }

    public class IntegrationAttributeValidation : BaseAttributeValidation
    {
        protected override bool CanApply(string type) => type == TYPE_INTEGRATION;

        protected override bool ValidateAttribute(AssetAttributeDto dto)
        {
            return dto.Payload == null
                || (dto.Payload.GetId() != Guid.Empty && !string.IsNullOrWhiteSpace(dto.Payload.MetricKey));
        }

        protected override bool ValidateMapping(AttributeMapping mapping)
        {
            return mapping.GetId() != Guid.Empty
                && mapping.GetAssetId() != Guid.Empty
                && mapping.GetAssetAttributeTemplateId() != Guid.Empty;
        }
    }

    public class CommandAttributeValidation : BaseAttributeValidation
    {
        protected override bool CanApply(string type) => type == TYPE_COMMAND;

        protected override bool ValidateAttribute(AssetAttributeDto dto)
        {
            return dto.Payload == null
                || (dto.Payload.GetId() != Guid.Empty && dto.Payload.RowVersion.HasValue && dto.Payload.RowVersion.Value != Guid.Empty);
        }

        protected override bool ValidateMapping(AttributeMapping mapping)
        {
            return mapping.GetId() != Guid.Empty
                && mapping.GetAssetId() != Guid.Empty
                && mapping.GetAssetAttributeTemplateId() != Guid.Empty
                && mapping.RowVersion.HasValue
                && mapping.RowVersion.Value != Guid.Empty;
        }
    }

    public class AliasAttributeValidation : BaseAttributeValidation
    {
        protected override bool CanApply(string type) => type == TYPE_ALIAS;

        protected override bool ValidateAttribute(AssetAttributeDto dto)
        {
            return dto.Payload == null
                || (dto.Payload.GetId() != Guid.Empty);
        }

        protected override bool ValidateMapping(AttributeMapping mapping)
        {
            return mapping.GetId() != Guid.Empty
                && mapping.GetAssetId() != Guid.Empty
                && mapping.GetAssetAttributeTemplateId() != Guid.Empty;
        }
    }

    public class RuntimeAttributeValidation : BaseAttributeValidation
    {
        protected override bool CanApply(string type) => type == TYPE_RUNTIME;

        protected override bool ValidateAttribute(AssetAttributeDto dto)
        {
            return dto.Payload == null
                || (dto.Payload.GetId() != Guid.Empty && ValidateExpression(dto.Payload));
        }

        private bool ValidateExpression(AttributeMapping payload)
        {
            return payload.EnabledExpression == false
                || (!string.IsNullOrWhiteSpace(payload.GetExpression()));
        }

        protected override bool ValidateMapping(AttributeMapping mapping)
        {
            return mapping.GetId() != Guid.Empty
                && mapping.GetAssetId() != Guid.Empty
                && mapping.GetAssetAttributeTemplateId() != Guid.Empty
                && ValidateExpression(mapping);
        }
    }
}
