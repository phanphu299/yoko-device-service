using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using Device.Application.Asset.Command;
using Device.Application.Constant;
using Device.Application.Enum;
using Device.Application.Service.Abstraction;
using Device.Application.Validation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Device.Application.Asset.Validation
{
    public class IntegrationAttributeValidator : BaseAssetAttributeValidator
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;

        public IntegrationAttributeValidator(IDeviceService deviceService,
                                             IDeviceTemplateService templateService,
                                             IHttpClientFactory httpClientFactory,
                                             ITenantContext tenantContext,
                                             IUomService uomService,
                                             IAssetService assetService,
                                             ILogger<BaseAttributeValidator> logger) : base(deviceService,
                                                                    templateService,
                                                                    uomService,
                                                                    assetService,
                                                                    httpClientFactory,
                                                                    tenantContext,
                                                                    logger)
        {
            _httpClientFactory = httpClientFactory;
            _tenantContext = tenantContext;
        }

        protected override async Task<IEnumerable<ValidationFailure>> ValidateAttributesAsync(Guid assetId, ValidatAttributeRequest command, IEnumerable<ValidatAttributeRequest> attributes, ValidationAction validationAction)
        {
            var failures = new List<ValidationFailure>();

            if (command != null)
            {
                failures.AddRange(await ValidateIntegrationDeviceAsync(command.DeviceIdIntegration, command.ChannelId));
                failures.AddRange(await ValidateIntegrationMetricAsync(command.MetricKeyIntegration, command.DeviceIdIntegration, command.ChannelId));
                failures.AddRange(await CheckExistChannelAsync(command.ChannelId));
                failures.AddRange(await ValidateUomAsync(command.UomId));
            }

            return failures;
        }

        protected override string AttributeType => AttributeTypeConstants.TYPE_INTEGRATION;

        private async Task<IEnumerable<ValidationFailure>> CheckExistChannelAsync(Guid? channelId)
        {
            var failures = new List<ValidationFailure>();

            if (channelId != null)
            {
                try
                {
                    var httpClient = _httpClientFactory.CreateClient(HttpClientNames.BROKER, _tenantContext);
                    var existChannelResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"bkr/integrations/{channelId}"));
                    if (existChannelResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.CHANNEL, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
                    }
                }
                catch (HttpRequestException)
                {
                    failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.CHANNEL, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
                }
            }

            return failures;
        }
    }
}
