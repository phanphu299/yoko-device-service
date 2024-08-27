using System;
using System.Collections.Generic;
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

namespace Device.Application.Asset.Validation
{
    public class DynamicAttributeValidator : BaseAssetAttributeValidator
    {
        public DynamicAttributeValidator(IDeviceService deviceService,
                                         IDeviceTemplateService templateService,
                                         IUomService uomService,
                                         IAssetService assetService,
                                         IHttpClientFactory httpClientFactory,
                                         ITenantContext tenantContext,
                                         ILogger<BaseAttributeValidator> logger) : base(deviceService,
                                                                templateService,
                                                                uomService,
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
                failures.AddRange(await ValidateDeviceAsync(command.DeviceId));
                failures.AddRange(await ValidateMetricAsync(command.MetricKey, command.DeviceId, command.DeviceTemplateId));
                failures.AddRange(await ValidateUomAsync(command.UomId));
            }

            return failures;
        }

        protected override string AttributeType => AttributeTypeConstants.TYPE_DYNAMIC;
    }
}
