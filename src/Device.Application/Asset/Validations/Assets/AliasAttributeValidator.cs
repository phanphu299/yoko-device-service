using System;
using System.Collections.Generic;
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

namespace Device.Application.Asset.Validation
{
    public class AliasAttributeValidator : BaseAssetAttributeValidator
    {
        private readonly AliasAssetAttributeHandler _aliasAssetAttributeHandler;

        public AliasAttributeValidator(IDeviceService deviceService,
                                       IDeviceTemplateService templateService,
                                       IUomService uomService,
                                       IAssetService assetService,
                                       IHttpClientFactory httpClientFactory,
                                       ITenantContext tenantContext,
                                       AliasAssetAttributeHandler aliasAssetAttributeHandler,
                                       ILogger<BaseAttributeValidator> logger) : base(deviceService,
                                                                templateService,
                                                                uomService,
                                                                assetService,
                                                                httpClientFactory,
                                                                tenantContext,
                                                                logger)
        {
            _aliasAssetAttributeHandler = aliasAssetAttributeHandler;
        }

        protected override async Task<IEnumerable<ValidationFailure>> ValidateAttributesAsync(Guid assetId, Command.ValidatAttributeRequest command, IEnumerable<ValidatAttributeRequest> attributes, ValidationAction validationAction)
        {
            var failures = new List<ValidationFailure>();

            if (command != null)
            {
                if (command.AliasAssetId == null)
                {
                    failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.ASSET, ExceptionErrorCode.DetailCode.ERROR_VALIDATION_SOME_ITEMS_DELETED));
                }
                else if (command.AliasAttributeId == null)
                {
                    failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.ALIAS_ATTRIBUTE, ExceptionErrorCode.DetailCode.ERROR_VALIDATION_SOME_ITEMS_DELETED));
                }
                else
                {
                    failures.AddRange(await ValidateAttributeAsync(command.AliasAssetId.Value, command.AliasAttributeId.Value));
                }
                failures.AddRange(await ValidateUomAsync(command.UomId));
                failures.AddRange(await CheckLoopAliasAsync(command.AttributeId, command.AliasAttributeId));
            }

            return failures;
        }

        protected override string AttributeType => AttributeTypeConstants.TYPE_ALIAS;

        private async Task<IEnumerable<ValidationFailure>> CheckLoopAliasAsync(Guid? attributeId, Guid? aliasAttributeId)
        {
            var failures = new List<ValidationFailure>();

            if (attributeId != null && aliasAttributeId != null)
            {
                var isCircleAlias = await _aliasAssetAttributeHandler.ValidateCircleAliasAsync(attributeId.Value, aliasAttributeId.Value);
                if (isCircleAlias)
                {
                    failures.Add(new ValidationFailure(ErrorPropertyConstants.AssetAttribute.ALIAS_ATTRIBUTE, MessageConstants.MESSAGE_ASSETS_SAVE_ASSET_LOOP_ALIAS));
                }
            }

            return failures;
        }
    }
}
