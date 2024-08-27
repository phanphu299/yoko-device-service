using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using Device.Application.Asset.Command;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using Device.Application.Validation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Device.Application.AssetTemplate.Validation
{
    public abstract class BaseAssetTemplateAttributeValidator : BaseAttributeValidator, IAssetTemplateAttributeValidator
    {
        protected readonly IAssetTemplateService _assetTemplateService;
        protected readonly IAssetAttributeService _assetAttributeService;

        public BaseAssetTemplateAttributeValidator(IDeviceService deviceService,
                                          IDeviceTemplateService templateService,
                                          IUomService uomService,
                                          IAssetAttributeService assetAttributeService,
                                          IAssetTemplateService assetAttributeTemplateService,
                                          IHttpClientFactory httpClientFactory,
                                          ITenantContext tenantContext,
                                          ILogger<BaseAttributeValidator> logger) : base(deviceService, templateService, uomService, httpClientFactory, tenantContext, logger)
        {
            _assetTemplateService = assetAttributeTemplateService;
            _assetAttributeService = assetAttributeService;
        }

        protected Task<IEnumerable<ValidationFailure>> ValidateTriggerAttributeAsync(IEnumerable<ValidatAttributeRequest> attributes, Guid triggerAttributeId)
        {
            var failures = new List<ValidationFailure>();
            try
            {
                if (!attributes.Any(x => x.Id == triggerAttributeId))
                {
                    failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.TRIGGER_ATTRIBUTE, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
                }
            }
            catch (EntityValidationException)
            {
                failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.ASSET, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
            }

            return Task.FromResult<IEnumerable<ValidationFailure>>(failures);
        }

        protected async Task<IEnumerable<ValidationFailure>> ValidateDuplicateNameAsync(Guid? assetTemplateId, ValidatAttributeRequest command)
        {
            var failures = new List<ValidationFailure>();
            if (assetTemplateId == null || assetTemplateId == Guid.Empty)
                return failures;

            var duplicatedName = await _assetTemplateService.IsDuplicationAttributeNameAsync(assetTemplateId.Value, new List<string> { command.Name });
            if (duplicatedName)
                failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.ASSET_ATTRIBUTE_NAME, ExceptionErrorCode.DetailCode.ERROR_VALIDATION_DUPLICATED));

            return failures;
        }

        protected async Task<IEnumerable<ValidationFailure>> ValidateAttributeIsUsingAsync(Guid attributeId)
        {
            var failures = new List<ValidationFailure>();

            try
            {
                await _assetTemplateService.CheckUsingAttributeAsync(attributeId);
            }
            catch (EntityValidationException ex)
            {
                failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.ATTRIBUTE, ex.ErrorCode)
                {
                    FormattedMessagePlaceholderValues = (Dictionary<string, object>)ex.Payload
                });
            }
            catch (EntityNotFoundException)
            {
                failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.ATTRIBUTE, MessageConstants.ASSET_ATTRIBUTE_NOT_FOUND));
            }

            return failures;
        }
    }
}
