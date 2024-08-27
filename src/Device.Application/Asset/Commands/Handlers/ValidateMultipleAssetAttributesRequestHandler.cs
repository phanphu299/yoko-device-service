using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Device.Application.Asset.Command.Model;
using Device.Application.Enum;
using Device.Application.Validation;
using Device.ApplicationExtension.Extension;
using MediatR;
using System;

namespace Device.Application.Asset.Command.Handler
{
    public class ValidateMultipleAssetAttributesRequestHandler : IRequestHandler<ValidateMultipleAssetAttributeList, List<ValidateMultipleAssetAttributeListResponse>>
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ITenantContext _tenantContext;

        public ValidateMultipleAssetAttributesRequestHandler(IServiceScopeFactory scopeFactory,
            ITenantContext tenantContext)
        {
            _scopeFactory = scopeFactory;
            _tenantContext = tenantContext;
        }

        public async Task<List<ValidateMultipleAssetAttributeListResponse>> Handle(ValidateMultipleAssetAttributeList request, CancellationToken cancellationToken)
        {
            foreach (ValidatAttributeRequest attribute in request.Attributes)
            {
                if (attribute.AttributeId == Guid.Empty)
                {
                    attribute.AttributeId = attribute.Id;
                }

                if (string.IsNullOrEmpty(attribute.DeviceId))
                {
                    attribute.DeviceId = attribute.CommandDeviceId;
                }

                if (string.IsNullOrEmpty(attribute.MetricKey))
                {
                    attribute.MetricKey = attribute.CommandMetricKey;
                }
            }

            List<ValidatAttributeRequest> validatedAttributes = request.Attributes.Skip(request.StartIndex).Take(request.BatchSize).ToList();

            var tasks = validatedAttributes.Select(x =>
                                {
                                    return ValidateSingleAttribute(x, request, _tenantContext);
                                });

            ValidateMultipleAssetAttributeListResponse[] result = await Task.WhenAll(tasks);
            return result.ToList();
        }

        private async Task<ValidateMultipleAssetAttributeListResponse> ValidateSingleAttribute(ValidatAttributeRequest attribute,
            ValidateMultipleAssetAttributeList request,
            ITenantContext tenantContextSource)
        {
            var scope = _scopeFactory.CreateScope();
            var tenantContext = scope.ServiceProvider.GetRequiredService<ITenantContext>();
            tenantContext.CopyFrom(tenantContextSource);
            var validatorHandler = scope.ServiceProvider.GetRequiredService<IDictionary<ValidationType, IAttributeValidator>>();

            var validationResponse = new ValidateMultipleAssetAttributeListResponse();
            if (validatorHandler.ContainsKey(request.ValidationType))
            {
                var validator = validatorHandler[request.ValidationType];
                var failures = await validator.ValidateAsync(attribute.AssetId, attribute, request.Attributes, request.ValidationAction);

                if (failures.Any())
                {
                    var errors = failures.Select(x => new ErrorField(x.PropertyName, x.ErrorMessage, x.FormattedMessagePlaceholderValues));
                    validationResponse.Properties = errors;
                }
            }

            validationResponse.AttributeId = attribute.Id;

            return validationResponse;
        }
    }
}
