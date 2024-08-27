using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Application.Asset.Command;
using Device.Application.Constant;
using Device.Application.Enum;
using Device.Application.Service.Abstraction;
using Device.Application.Validation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Device.Application.AssetTemplate.Validation
{
    public class DynamicAttributeValidator : BaseAssetTemplateAttributeValidator
    {
        public DynamicAttributeValidator(IDeviceService deviceService,
                                         IDeviceTemplateService templateService,
                                         IUomService uomService,
                                         IAssetTemplateService assetService,
                                         IAssetAttributeService assetAttributesService,
                                         IHttpClientFactory httpClientFactory,
                                         ITenantContext tenantContext,
                                         ILogger<BaseAttributeValidator> logger) : base(deviceService,
                                                                templateService,
                                                                uomService,
                                                                assetAttributesService,
                                                                assetService,
                                                                httpClientFactory,
                                                                tenantContext,
                                                                logger)
        {
        }

        protected override async Task<IEnumerable<ValidationFailure>> ValidateAttributesAsync(Guid assetId, ValidatAttributeRequest command, IEnumerable<ValidatAttributeRequest> attributes, ValidationAction validationAction)
        {
            var failures = new List<ValidationFailure>();

            if (command != null)
            {
                switch (validationAction)
                {
                    case ValidationAction.Upsert:
                        var deviceTemplateId = command.DeviceTemplateId ?? Guid.Empty;
                        failures.AddRange(await ValidateDeviceTemplateAsync(deviceTemplateId));
                        failures.AddRange(await ValidateMetricAsync(command.MetricKey, command.DeviceId, command.DeviceTemplateId));
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

        protected override string AttributeType => AttributeTypeConstants.TYPE_DYNAMIC;
    }
}
