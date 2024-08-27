using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Asset.Command;
using Device.Application.Constant;
using Device.Application.Device.Command;
using Device.Application.Enum;
using Device.Application.Model;
using Device.Application.Service.Abstraction;
using Device.Application.Uom.Command;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Device.Application.Validation
{
    public abstract class BaseAttributeValidator : IAttributeValidator
    {
        protected BaseAttributeValidator _nextValidator;
        protected readonly IDeviceService _deviceService;
        protected readonly IDeviceTemplateService _deviceTemplateService;
        protected readonly IUomService _uomService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;
        private readonly ILogger<BaseAttributeValidator> _logger;

        protected BaseAttributeValidator(IDeviceService deviceService,
                                          IDeviceTemplateService templateService,
                                          IUomService uomService,
                                           IHttpClientFactory httpClientFactory,
                                           ITenantContext tenantContext,
                                           ILogger<BaseAttributeValidator> logger)
        {
            _deviceService = deviceService;
            _deviceTemplateService = templateService;
            _uomService = uomService;
            _httpClientFactory = httpClientFactory;
            _tenantContext = tenantContext;
            _logger = logger;
        }

        public void SetNextValidator(BaseAttributeValidator nextValidator)
        {
            _nextValidator = nextValidator;
        }

        protected async Task<IEnumerable<ValidationFailure>> ValidateIntegrationDeviceAsync(string deviceId, Guid? channelId)
        {
            var failures = new List<ValidationFailure>();

            if (channelId == null)
            {
                return failures;
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient(HttpClientNames.BROKER, _tenantContext);
                var getDeviceResponse = await httpClient.GetAsync($"bkr/integrations/{channelId}/fetch?type=devices");
                if (getDeviceResponse.IsSuccessStatusCode)
                {
                    var content = await getDeviceResponse.Content.ReadAsByteArrayAsync();
                    var devices = content.Deserialize<BaseSearchResponse<IntegrationDevice>>();

                    if (!devices.Data.Any(x => x.Id == deviceId))
                    {
                        failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.DEVICE, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
                    }
                }
            }
            catch (EntityNotFoundException)
            {
                failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.DEVICE, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
            }

            return failures;
        }

        protected async Task<IEnumerable<ValidationFailure>> ValidateIntegrationMetricAsync(string metricKey, string deviceId, Guid? channelId)
        {
            var failures = new List<ValidationFailure>();

            if (channelId == null)
            {
                return failures;
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient(HttpClientNames.BROKER, _tenantContext);
                var getMetricsResponse = await httpClient.GetAsync($"bkr/integrations/{channelId}/fetch?type=metrics&data={deviceId}");
                if (getMetricsResponse.IsSuccessStatusCode)
                {
                    var content = await getMetricsResponse.Content.ReadAsByteArrayAsync();
                    var metrics = content.Deserialize<BaseSearchResponse<IntegrationMetric>>();

                    if (!metrics.Data.Any(x => x.Name == metricKey))
                    {
                        failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.METRIC, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
                    }
                }
            }
            catch (EntityNotFoundException)
            {
                failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.METRIC, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
            }

            return failures;
        }

        protected async Task<IEnumerable<ValidationFailure>> ValidateDeviceAsync(string deviceId)
        {
            var failures = new List<ValidationFailure>();
            try
            {
                await _deviceService.CheckExistDevicesAsync(new CheckExistDevice(new string[] { deviceId }), CancellationToken.None);
            }
            catch (EntityNotFoundException)
            {
                failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.DEVICE, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation(ex, ex.Message);
            }

            return failures;
        }

        protected async Task<IEnumerable<ValidationFailure>> ValidateDeviceTemplateAsync(Guid deviceTemplateId)
        {
            var failures = new List<ValidationFailure>();

            try
            {
                await _deviceTemplateService.CheckExistDeviceTemplatesAsync(new Template.Command.CheckExistTemplate(new Guid[] { deviceTemplateId }), CancellationToken.None);
            }
            catch (EntityNotFoundException)
            {
                failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.DEVICE_TEMPLATE, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
            }

            return failures;
        }

        protected async Task<IEnumerable<ValidationFailure>> ValidateMetricAsync(string metricKey, string deviceId, Guid? deviceTemplateId)
        {
            var failures = new List<ValidationFailure>();

            try
            {
                bool existMetric = false;
                if (!string.IsNullOrWhiteSpace(metricKey))
                {
                    existMetric = await _deviceService.CheckExistMetricByDeviceIdAsync(metricKey, deviceId);
                }

                if (deviceTemplateId != null)
                {
                    existMetric = await _deviceTemplateService.CheckExistMetricByTemplateIdAsync(metricKey, deviceTemplateId);
                }

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

        protected async Task<IEnumerable<ValidationFailure>> ValidateUomAsync(int uomId)
        {
            var failures = new List<ValidationFailure>();

            if (uomId != 0)
            {
                try
                {
                    await _uomService.CheckExistUomsAsync(new CheckExistUom(new List<int>() { uomId }), CancellationToken.None);
                }
                catch (EntityNotFoundException)
                {
                    failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.UOM, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
                }
            }

            return failures;
        }

        public async Task<IEnumerable<ValidationFailure>> ValidateAsync(Guid assetId, ValidatAttributeRequest command, IEnumerable<ValidatAttributeRequest> attributes, ValidationAction validationAction)
        {
            if (CanApply(command.AttributeType))
            {
                return await ValidateAttributesAsync(assetId, command, attributes, validationAction);
            }
            else if (_nextValidator != null)
            {
                return await _nextValidator.ValidateAsync(assetId, command, attributes, validationAction);
            }

            return Enumerable.Empty<ValidationFailure>();
        }

        public bool CanApply(string type)
        {
            return AttributeType == type;
        }

        protected abstract string AttributeType { get; }

        protected abstract Task<IEnumerable<ValidationFailure>> ValidateAttributesAsync(Guid assetId, ValidatAttributeRequest command, IEnumerable<ValidatAttributeRequest> attributes, ValidationAction validationAction);
    }
}
