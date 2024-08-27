using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Validation.Abstraction;
using Device.Application.Constant;
using Device.Application.Repository;
using Newtonsoft.Json.Linq;
using AHI.Infrastructure.Cache.Abstraction;
using Device.ApplicationExtension.Extension;
using AHI.Infrastructure.MultiTenancy.Internal;

namespace Device.Application.Service
{
    public class CommandAssetAttributeHandler : BaseAssetAttributeHandler
    {
        private readonly IDynamicValidator _dynamicValidator;
        private readonly IReadDeviceRepository _readDeviceRepository;
        private readonly ICache _cache;

        public CommandAssetAttributeHandler(IDynamicValidator dynamicValidator
            , IAssetUnitOfWork repository
            , IAuditLogService auditLogService
            , IDomainEventDispatcher domainEventDispatcher
            , ITenantContext tenantContext
            , IReadAssetAttributeTemplateRepository readAssetAttributeTemplateRepository
            , IReadAssetAttributeRepository readAssetAttributeRepository
            , IReadAssetRepository readAssetRepository
            , IReadDeviceRepository readDeviceRepository, ICache cache) : base(repository, auditLogService, domainEventDispatcher, tenantContext, readAssetAttributeRepository, readAssetRepository, readAssetAttributeTemplateRepository)
        {
            _dynamicValidator = dynamicValidator;
            _readDeviceRepository = readDeviceRepository;
            _cache = cache;
        }

        public override bool CanApply(string attributeType)
        {
            return attributeType == AttributeTypeConstants.TYPE_COMMAND;
        }

        /// <param name="attribute">processing attribute</param>
        /// <param name="inputAttributes">all attributes related to asset</param>
        protected override async Task<Domain.Entity.AssetAttribute> AddAttributeAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken, bool ignoreValidation = false)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var commandPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeCommand>();
            await _dynamicValidator.ValidateAsync(commandPayload, cancellationToken);
            var entity = Asset.Command.AssetAttribute.Create(attribute);
            entity.CreatedUtc = attribute.CreatedUtc;
            entity.SequentialNumber = attribute.SequentialNumber;
            entity.AssetAttributeCommand = AssetAttributeCommand.Create(commandPayload);
            if (!ignoreValidation)
            {
                await ValidateCommandAttributeAsync(entity);
            }
            
            return await _unitOfWork.AssetAttributes.AddEntityAsync(entity);
        }

        /// <param name="attribute">processing attribute</param>
        /// <param name="inputAttributes">all attributes related to asset</param>
        protected override async Task<Domain.Entity.AssetAttribute> UpdateAttributeAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var commandPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeCommand>();
            await _dynamicValidator.ValidateAsync(commandPayload, cancellationToken);
            var entity = Asset.Command.AssetAttribute.Create(attribute);
            entity.AssetAttributeCommand = AssetAttributeCommand.Create(commandPayload);
            await ValidateCommandAttributeAsync(entity);

            var asset = await _readAssetRepository.OnlyAssetAsQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.AssetId);
            if (asset == null)
            {
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.AssetAttribute.AssetId));
            }

            var trackingEntity = await _unitOfWork.AssetAttributes.AsQueryable().Include(x => x.AssetAttributeCommand).FirstOrDefaultAsync(x => x.Id == entity.Id);
            if (trackingEntity == null)
                throw new EntityNotFoundException(detailCode: MessageConstants.ASSET_ATTRIBUTE_NOT_FOUND);

            _unitOfWork.AssetAttributes.ProcessUpdate(entity, trackingEntity);
            trackingEntity.AssetAttributeCommand.DeviceId = entity.AssetAttributeCommand.DeviceId;
            trackingEntity.AssetAttributeCommand.MetricKey = entity.AssetAttributeCommand.MetricKey;
            trackingEntity.AssetAttributeCommand.UpdatedUtc = System.DateTime.UtcNow;

             
            var result = await _unitOfWork.AssetAttributes.UpdateEntityAsync(trackingEntity);
            var triggerKeyToRemove = CacheKey.AssetRuntimeTriggerPattern.GetCacheKey(_tenantContext.ProjectId);
            await _cache.DeleteHashByKeyAsync(triggerKeyToRemove, entity.Id.ToString());
            return result;
        }

        private async Task ValidateCommandAttributeAsync(Domain.Entity.AssetAttribute attribute)
        {
            //when clone asset device id and metrickey are empty
            if (attribute.AssetAttributeCommand.DeviceId == null && attribute.AssetAttributeCommand.MetricKey == null)
                return;

            var device = await _readDeviceRepository.AsQueryable().AsNoTracking()
                    .Include(x => x.Template).ThenInclude(x => x.Bindings)
                    .FirstOrDefaultAsync(x => x.Id == attribute.AssetAttributeCommand.DeviceId);

            if (device == null)
            {
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.AssetAttributeCommand.DeviceId));
            }

            var metricUsing = (await _readAssetAttributeRepository.AsQueryable()
                                .Include(x => x.AssetAttributeCommand)
                                .AnyAsync(x => x.AssetAttributeCommand.MetricKey == attribute.AssetAttributeCommand.MetricKey
                                && x.AssetAttributeCommand.DeviceId == attribute.AssetAttributeCommand.DeviceId
                                && x.Id != attribute.Id))
                            || (await _readAssetRepository.OnlyAssetAsQueryable().Include(x => x.AssetAttributeCommandMappings)
                                .SelectMany(x => x.AssetAttributeCommandMappings)
                                .AnyAsync(x => x.MetricKey == attribute.AssetAttributeCommand.MetricKey
                                && x.DeviceId == attribute.AssetAttributeCommand.DeviceId
                                && x.Id != attribute.Id))
                            || _unitOfWork.AssetAttributes.UnSaveAttributes.Where(x => x.AssetAttributeCommand != null)
                                .Any(x => x.AssetAttributeCommand.DeviceId == attribute.AssetAttributeCommand.DeviceId && x.AssetAttributeCommand.MetricKey == attribute.AssetAttributeCommand.MetricKey);
            if (metricUsing)
            {
                throw new EntityValidationException();
            }

            var metric = device.Template.Bindings.FirstOrDefault(x => x.Key == attribute.AssetAttributeCommand.MetricKey);
            if (metric == null)
            {
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.AssetAttributeCommand.MetricKey));
            }

            // set the dataType of this attribute base on data type of device binding metric
            attribute.DataType = metric.DataType;
        }
    }

    internal class AssetAttributeCommand
    {
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public string Value { get; set; }
        public static Domain.Entity.AssetAttributeCommand Create(AssetAttributeCommand model)
        {
            return new Domain.Entity.AssetAttributeCommand
            {
                DeviceId = model.DeviceId,
                MetricKey = model.MetricKey,
                Value = model.Value,
                RowVersion = Guid.NewGuid()
            };
        }
    }
}
