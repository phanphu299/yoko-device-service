using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Application.Asset.Command;
using Device.Application.Constant;
using Device.Application.Enum;
using Device.Application.Service;
using Device.Application.Service.Abstraction;
using Device.Application.Validation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Device.Application.AssetTemplate.Validation
{
    public class StaticAttributeValidator : BaseAssetTemplateAttributeValidator
    {
        private readonly StaticAssetAttributeHandler _staticHandler;

        public StaticAttributeValidator(IDeviceService deviceService,
                                         IDeviceTemplateService templateService,
                                         IUomService uomService,
                                         IAssetTemplateService assetService,
                                         IAssetAttributeService assetAttributeService,
                                         IHttpClientFactory httpClientFactory,
                                         ITenantContext tenantContext,
                                         StaticAssetAttributeHandler staticHandler,
                                         ILogger<BaseAttributeValidator> logger) : base(deviceService,
                                                                templateService,
                                                                uomService,
                                                                assetAttributeService,
                                                                assetService,
                                                                httpClientFactory,
                                                                tenantContext,
                                                                logger)
        {
            _staticHandler = staticHandler;
        }

        protected override string AttributeType => AttributeTypeConstants.TYPE_STATIC;

        protected override async Task<IEnumerable<ValidationFailure>> ValidateAttributesAsync(Guid assetId, ValidatAttributeRequest command, IEnumerable<ValidatAttributeRequest> attributes, ValidationAction validationAction)
        {
            var failures = new List<ValidationFailure>();

            if (command != null)
            {
                switch (validationAction)
                {
                    case ValidationAction.Upsert:
                        var result = _staticHandler.ValidateValue(command.Value, command.DataType);
                        if (!result)
                        {
                            failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.ASSET, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
                        }

                        failures.AddRange(await ValidateUomAsync(command.UomId));
                        if (attributes != null && attributes.Any())
                        {
                            var assetTemplateId = attributes.FirstOrDefault(x => x.AssetTemplateId != null)?.AssetTemplateId ?? Guid.Empty;
                            failures.AddRange(await ValidateDuplicateNameAsync(assetTemplateId, command));
                        }
                        break;

                    case ValidationAction.Delete:
                        failures.AddRange(await ValidateAttributeIsUsingAsync(command.AttributeId));
                        break;

                    default:
                        break;
                }
            }

            return failures;
        }
    }
}
