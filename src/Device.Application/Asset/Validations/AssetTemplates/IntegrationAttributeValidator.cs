using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Device.Application.AssetTemplate.Validation
{
    public class IntegrationAttributeValidator : BaseAssetTemplateAttributeValidator
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITenantContext _tenantContext;

        public IntegrationAttributeValidator(IDeviceService deviceService,
                                             IDeviceTemplateService templateService,
                                             IHttpClientFactory httpClientFactory,
                                             ITenantContext tenantContext,
                                             IUomService uomService,
                                             IAssetAttributeService assetAttributeService,
                                             IAssetTemplateService assetService,
                                             ILogger<BaseAttributeValidator> logger) : base(deviceService,
                                                                    templateService,
                                                                    uomService,
                                                                    assetAttributeService,
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
                switch (validationAction)
                {
                    case ValidationAction.Upsert:
                        failures.AddRange(await ValidateIntegrationDeviceAsync(command.DeviceId, command.ChannelId));
                        failures.AddRange(await ValidateIntegrationMetricAsync(command.MetricKey, command.DeviceId, command.ChannelId));
                        failures.AddRange(await CheckExistChannelAsync(command.ChannelId));
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
