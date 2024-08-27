using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Application.Asset.Command;
using Device.Application.Asset.Command.Model;
using Device.Application.Constant;
using Device.Application.Enum;
using Device.Application.Service;
using Device.Application.Service.Abstraction;
using Device.Application.Validation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Device.Application.Asset.Validation
{
    public class RuntimeAttributeValidator : BaseAssetAttributeValidator
    {
        private readonly RuntimeAssetAttributeHandler _runtimeHandler;

        public RuntimeAttributeValidator(IDeviceService deviceService,
                                         IDeviceTemplateService templateService,
                                         IUomService uomService,
                                         IAssetService assetService,
                                         IHttpClientFactory httpClientFactory,
                                         ITenantContext tenantContext,
                                         RuntimeAssetAttributeHandler runtimeHandler,
                                         ILogger<BaseAttributeValidator> logger) : base(deviceService,
                                                                templateService,
                                                                uomService,
                                                                assetService,
                                                                httpClientFactory,
                                                                tenantContext,
                                                                logger)
        {
            _runtimeHandler = runtimeHandler;
        }

        protected override string AttributeType => AttributeTypeConstants.TYPE_RUNTIME;

        protected override async Task<IEnumerable<ValidationFailure>> ValidateAttributesAsync(Guid assetId, ValidatAttributeRequest command, IEnumerable<ValidatAttributeRequest> attributes, ValidationAction validationAction)
        {
            var failures = new List<ValidationFailure>();

            if (command != null)
            {
                failures.AddRange(await ValidateUomAsync(command.UomId));
                if (command.EnabledExpression)
                    failures.AddRange(await ValidateExpressionAsync(command, attributes));
            }

            return failures;
        }

        private async Task<List<ValidationFailure>> ValidateExpressionAsync(ValidatAttributeRequest command, IEnumerable<ValidatAttributeRequest> attributes)
        {
            var failures = new List<ValidationFailure>();
            var expressionRequestValidation = new AssetTemplateAttributeValidationRequest()
            {
                DataType = command.DataType,
                Expression = command.Expression,
                Id = command.Id,
                Attributes = attributes.Select(att => new AssetTemplateAttributeValidationRequest
                {
                    Id = att.Id,
                    DataType = att.DataType
                })
            };
            try
            {
                var (result, _, _) = _runtimeHandler.ValidateExpression(expressionRequestValidation);
                if (!result)
                {
                    failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.EXPRESSION, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
                }
                if (command.IsTriggerVisibility)
                {
                    failures.AddRange(await ValidateTriggerAttributeAsync(attributes, command.TriggerAttributeId.Value));
                }
            }
            catch (EntityValidationException ex)
            {
                switch (ex.DetailCode)
                {
                    case ExceptionErrorCode.DetailCode.ERROR_VALIDATION_SOME_ITEMS_DELETED:
                        failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.ATTRIBUTE, ExceptionErrorCode.DetailCode.ERROR_VALIDATION_SOME_ITEMS_DELETED));
                        break;

                    default:
                        failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.EXPRESSION, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
                        break;
                }
            }
            return failures;
        }
    }
}
