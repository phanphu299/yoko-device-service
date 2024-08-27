using System;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Constant;
using Device.Application.Repository;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Validation.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.Audit.Service.Abstraction;
using System.Collections.Generic;

namespace Device.Application.Service
{
    public class IntegrationAssetAttributeHandler : BaseAssetAttributeHandler
    {
        private readonly IDynamicValidator _dynamicValidator;
        private readonly IReadUomRepository _readUomRepository;
        public IntegrationAssetAttributeHandler(
            IDynamicValidator dynamicValidator,
            IAssetUnitOfWork repository,
            IAuditLogService auditLogService,
            IDomainEventDispatcher domainEventDispatcher,
            ITenantContext tenantContext,
            IReadAssetAttributeRepository readAssetAttributeRepository,
            IReadAssetRepository readAssetRepository,
            IReadUomRepository readUomRepository,
            IReadAssetAttributeTemplateRepository readAssetAttributeTemplateRepository
            ) : base(repository, auditLogService, domainEventDispatcher, tenantContext, readAssetAttributeRepository, readAssetRepository, readAssetAttributeTemplateRepository)
        {
            _dynamicValidator = dynamicValidator;
            _readUomRepository = readUomRepository;
        }

        /// <param name="attribute">processing attribute</param>
        /// <param name="inputAttributes">all attributes related to asset</param>
        protected override async Task<Domain.Entity.AssetAttribute> AddAttributeAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken, bool ignoreValidation = false)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var integrationPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeIntegration>();
            await _dynamicValidator.ValidateAsync(integrationPayload, cancellationToken);
            var entity = Asset.Command.AssetAttribute.Create(attribute);
            entity.CreatedUtc = attribute.CreatedUtc;
            entity.SequentialNumber = attribute.SequentialNumber;
            if (!ignoreValidation)
            {
                await ValidateExistUomByIdAsync(entity);
            }

            entity.AssetAttributeIntegration = AssetAttributeIntegration.Create(integrationPayload);
            return await _unitOfWork.AssetAttributes.AddEntityAsync(entity);
        }

        /// <param name="attribute">processing attribute</param>
        /// <param name="inputAttributes">all attributes related to asset</param>
        protected override async Task<Domain.Entity.AssetAttribute> UpdateAttributeAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var integrationPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeIntegration>();
            await _dynamicValidator.ValidateAsync(integrationPayload, cancellationToken);
            var entity = Asset.Command.AssetAttribute.Create(attribute);
            await ValidateExistUomByIdAsync(entity);
            entity.AssetAttributeIntegration = AssetAttributeIntegration.Create(integrationPayload);

            var trackingEntity = await _unitOfWork.AssetAttributes.AsQueryable().Include(x => x.AssetAttributeIntegration).FirstOrDefaultAsync(x => x.Id == entity.Id);

            if (trackingEntity == null)
                throw new EntityNotFoundException();

            _unitOfWork.AssetAttributes.ProcessUpdate(entity, trackingEntity);
            trackingEntity.AssetAttributeIntegration.IntegrationId = entity.AssetAttributeIntegration.IntegrationId;
            trackingEntity.AssetAttributeIntegration.DeviceId = entity.AssetAttributeIntegration.DeviceId;
            trackingEntity.AssetAttributeIntegration.MetricKey = entity.AssetAttributeIntegration.MetricKey;
            return await _unitOfWork.AssetAttributes.UpdateEntityAsync(trackingEntity);
        }

        public override bool CanApply(string attributeType)
        {
            return attributeType == AttributeTypeConstants.TYPE_INTEGRATION;
        }

        private async Task ValidateExistUomByIdAsync(Domain.Entity.AssetAttribute attribute)
        {
            if (attribute.UomId.HasValue)
            {
                var uom = await _readUomRepository.AsQueryable().AsNoTracking().AnyAsync(x => x.Id == attribute.UomId);
                if (!uom)
                    throw EntityValidationExceptionHelper.GenerateException(nameof(attribute.UomId), ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
            }
        }
    }
    internal class AssetAttributeIntegration
    {
        public Guid IntegrationId { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }

        internal static Domain.Entity.AssetAttributeIntegration Create(AssetAttributeIntegration integrationPayload)
        {
            return new Domain.Entity.AssetAttributeIntegration()
            {
                IntegrationId = integrationPayload.IntegrationId,
                DeviceId = integrationPayload.DeviceId,
                MetricKey = integrationPayload.MetricKey,
            };
        }
    }
}