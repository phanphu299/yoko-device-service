using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Application.Asset.Command;
using Device.Application.Constant;
using Device.Application.Device.Command;
using Device.Application.Enum;
using Device.Application.Service.Abstraction;
using Device.Application.Validation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Device.Application.Asset.Validation
{
    public class CommandAttributeValidator : BaseAssetAttributeValidator
    {
        public CommandAttributeValidator(IDeviceService deviceService,
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
                failures.AddRange(await CheckExistBindingsAsync(command.MetricKey, command.DeviceId));
                failures.AddRange(await ValidateBindingsAsync(command.MetricKey, command.DeviceId, command.AttributeId));
                failures.AddRange(await ValidateUomAsync(command.UomId));
            }

            return failures;
        }

        protected override string AttributeType => AttributeTypeConstants.TYPE_COMMAND;

        protected async Task<IEnumerable<ValidationFailure>> CheckExistBindingsAsync(string metricKey, string deviceId)
        {
            var failures = new List<ValidationFailure>();

            try
            {
                var existDevice = await _deviceService.FindByIdAsync(new GetDeviceById(deviceId), CancellationToken.None);
                if (existDevice == null)
                {
                    failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.DEVICE, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));

                    return failures;
                }

                var existMetric = await _deviceTemplateService.CheckExistBindingsByTemplateIdAsync(metricKey, existDevice.Template.Id);
                if (!existMetric)
                {
                    failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.METRIC, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
                }
            }
            catch (EntityNotFoundException)
            {
                failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.METRIC, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
            }

            return failures;
        }

        protected async Task<IEnumerable<ValidationFailure>> ValidateBindingsAsync(string metricKey, string deviceId, Guid commandAttributeId)
        {
            var failures = new List<ValidationFailure>();

            try
            {
                var existBindings = await _assetService.ValidateDeviceBindingAsync(new ValidateDeviceBindings()
                {
                    ValidateBindings = new List<ValidateDeviceBinding>()
                    {
                       new ValidateDeviceBinding()
                       {
                           CommandAttributeId = commandAttributeId,
                           DeviceId = deviceId,
                           MetricKey = metricKey
                       }
                    }
                }, CancellationToken.None);

                if (!existBindings.Any() || existBindings.Any(x => !x.IsValid))
                {
                    failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.METRIC, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
                }
            }
            catch (EntityNotFoundException)
            {
                failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.METRIC, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
            }

            return failures;
        }
    }
}
