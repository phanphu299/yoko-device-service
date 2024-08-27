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
using Device.Application.Service.Abstraction;
using Device.Application.Validation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Device.Application.Asset.Validation
{
    public abstract class BaseAssetAttributeValidator : BaseAttributeValidator, IAssetAttributeValidator
    {
        protected readonly IAssetService _assetService;

        public BaseAssetAttributeValidator(IDeviceService deviceService,
                                          IDeviceTemplateService templateService,
                                          IUomService uomService,
                                          IAssetService assetService,
                                          IHttpClientFactory httpClientFactory,
                                          ITenantContext tenantContext,
                                          ILogger<BaseAttributeValidator> logger) : base(deviceService, templateService, uomService, httpClientFactory, tenantContext, logger)
        {
            _assetService = assetService;
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

        protected async Task<IEnumerable<ValidationFailure>> ValidateAttributeAsync(Guid assetId, Guid attributeId)
        {
            var failures = new List<ValidationFailure>();
            var existAsset = new Command.Model.GetAssetDto();
            try
            {
                existAsset = await _assetService.FindAssetByIdAsync(new GetAssetById(assetId), CancellationToken.None);
                if (!existAsset.Attributes.Any(x => x.Id == attributeId))
                {
                    failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.ATTRIBUTE, ExceptionErrorCode.DetailCode.ERROR_VALIDATION_SOME_ITEMS_DELETED));
                }
            }
            catch (EntityValidationException ex)
            {
                switch (ex.DetailCode)
                {
                    case ExceptionErrorCode.DetailCode.ERROR_VALIDATION_SOME_ITEMS_DELETED:
                        failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.ASSET, ExceptionErrorCode.DetailCode.ERROR_VALIDATION_SOME_ITEMS_DELETED));
                        break;
                    
                    default:
                        failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.ASSET, ExceptionErrorCode.ERROR_ENTITY_VALIDATION));
                        break;
                }
            }

            return failures;
        }
    }
}
