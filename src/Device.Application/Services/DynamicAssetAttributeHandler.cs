using System.Linq;
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
using AHI.Infrastructure.Audit.Constant;
using System.Collections.Generic;

namespace Device.Application.Service
{
    public class DynamicAssetAttributeHandler : BaseAssetAttributeHandler
    {
        private readonly IDynamicValidator _dynamicValidator;
        private readonly IDeviceUnitOfWork _deviceUnitOfWork;
        private readonly IReadDeviceRepository _readDeviceRepository;
        private readonly IReadUomRepository _readUomRepository;
        public DynamicAssetAttributeHandler(
            IDynamicValidator dynamicValidator,
            IAssetUnitOfWork repository,
            IDeviceUnitOfWork deviceUnitOfWork,
            IReadDeviceRepository readDeviceRepository,
            IAuditLogService auditLogService,
            IDomainEventDispatcher domainEventDispatcher,
            ITenantContext tenantContext,
            IReadAssetAttributeRepository readAssetAttributeRepository,
            IReadAssetRepository readAssetRepository,
            IReadUomRepository readUomRepository,
            IReadAssetAttributeTemplateRepository readAssetAttributeTemplateRepository) : base(repository, auditLogService, domainEventDispatcher, tenantContext, readAssetAttributeRepository, readAssetRepository, readAssetAttributeTemplateRepository)
        {
            _dynamicValidator = dynamicValidator;
            _deviceUnitOfWork = deviceUnitOfWork;
            _readUomRepository = readUomRepository;
            _readDeviceRepository = readDeviceRepository;
        }

        /// <param name="attribute">processing attribute</param>
        /// <param name="inputAttributes">all attributes related to asset</param>
        protected override async Task<Domain.Entity.AssetAttribute> AddAttributeAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken, bool ignoreValidation = false)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var dynamicPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeDynamic>();
            await _dynamicValidator.ValidateAsync(dynamicPayload, cancellationToken);
            var entity = Asset.Command.AssetAttribute.Create(attribute);
            if (entity.DataType != DataTypeConstants.TYPE_INTEGER && entity.DataType != DataTypeConstants.TYPE_DOUBLE)
            {
                entity.DecimalPlace = null;
                entity.ThousandSeparator = null;
            }
            entity.CreatedUtc = attribute.CreatedUtc;
            entity.SequentialNumber = attribute.SequentialNumber;
            entity.AssetAttributeDynamic = AssetAttributeDynamic.Create(dynamicPayload);
            if (!ignoreValidation)
            {
                await ValidateDynamicAttributeAsync(entity);
            }

            return await _unitOfWork.AssetAttributes.AddEntityAsync(entity);
        }

        /// <param name="attribute">processing attribute</param>
        /// <param name="inputAttributes">all attributes related to asset</param>
        protected override async Task<Domain.Entity.AssetAttribute> UpdateAttributeAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var dynamicPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeDynamic>();
            await _dynamicValidator.ValidateAsync(dynamicPayload, cancellationToken);
            var entity = Asset.Command.AssetAttribute.Create(attribute);
            entity.AssetAttributeDynamic = AssetAttributeDynamic.Create(dynamicPayload);
            await ValidateDynamicAttributeAsync(entity);

            var trackingEntity = await _unitOfWork.AssetAttributes.AsQueryable().Include(x => x.AssetAttributeDynamic).FirstOrDefaultAsync(x => x.Id == entity.Id);
            if (trackingEntity == null)
                throw new EntityNotFoundException();

            _unitOfWork.AssetAttributes.ProcessUpdate(entity, trackingEntity);
            trackingEntity.AssetAttributeDynamic.DeviceId = entity.AssetAttributeDynamic.DeviceId;
            trackingEntity.AssetAttributeDynamic.MetricKey = entity.AssetAttributeDynamic.MetricKey;
            trackingEntity.AssetAttributeDynamic.UpdatedUtc = System.DateTime.UtcNow;
            if (entity.DataType != DataTypeConstants.TYPE_INTEGER && entity.DataType != DataTypeConstants.TYPE_DOUBLE)
            {
                trackingEntity.DecimalPlace = null;
                trackingEntity.ThousandSeparator = null;
            }
            return await _unitOfWork.AssetAttributes.UpdateEntityAsync(trackingEntity);
        }
        private async Task ValidateDynamicAttributeAsync(Domain.Entity.AssetAttribute attribute)
        {
            var device = await _readDeviceRepository.FindAsync(attribute.AssetAttributeDynamic.DeviceId);

            if (device == null)
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET_ATTRIBUTES, ActionType.Add, ActionStatus.Fail, attribute.Id,
                                            attribute.Name, payload: attribute);
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.AssetAttributeDynamic.DeviceId));
            }

            if (attribute.UomId.HasValue)
            {
                var uom = await _readUomRepository.AsQueryable().AsNoTracking().AnyAsync(x => x.Id == attribute.UomId);
                if (!uom)
                    throw EntityValidationExceptionHelper.GenerateException(nameof(attribute.UomId), ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
            }

            var deviceTemplate = await _deviceUnitOfWork.DeviceTemplates.FindAsync(device.TemplateId);
            var metric = deviceTemplate.Payloads.SelectMany(x => x.Details).FirstOrDefault(x => x.Key == attribute.AssetAttributeDynamic.MetricKey);
            // var deviceExists = await _deviceMetricRepository.ExistAsync(deviceMetric.DeviceId, deviceMetric.MetricId);
            if (metric == null)
            {
                await _auditLogService.SendLogAsync(ActivityEntityAction.ASSET_ATTRIBUTES, ActionType.Add, ActionStatus.Fail, attribute.Id, attribute.Name, payload: attribute);
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.AssetAttributeDynamic.MetricKey));
            }
            // set the dataType of this attribute base on data type of device metric
            attribute.DataType = metric.DataType;
        }
        public override bool CanApply(string attributeType)
        {
            return attributeType == AttributeTypeConstants.TYPE_DYNAMIC;
        }
        // protected override Task<Domain.Entity.AssetAttribute> CloneEntityAsync(Domain.Entity.Asset asset, Domain.Entity.AssetAttribute cloningAttribute, Domain.Entity.AssetAttribute attribute)
        // {

        //     attribute.AssetAttributeDynamic = new Domain.Entity.AssetAttributeDynamic
        //     {
        //         AssetAttributeId = attribute.Id,
        //         DeviceId = cloningAttribute.AssetAttributeDynamic.DeviceId,
        //         MetricKey = cloningAttribute.AssetAttributeDynamic.MetricKey
        //     };

        //     return Task.FromResult(attribute);
        // }
    }
    internal class AssetAttributeDynamic
    {
        //public int DeviceTemplateId { get; set; }
        public string MetricKey { get; set; }
        public string DeviceId { get; set; }
        internal static Domain.Entity.AssetAttributeDynamic Create(AssetAttributeDynamic dynamicPayload)
        {
            return new Domain.Entity.AssetAttributeDynamic()
            {
                //device = integrationPayload.IntegrationId,
                DeviceId = dynamicPayload.DeviceId,
                MetricKey = dynamicPayload.MetricKey,
            };
        }
    }
}