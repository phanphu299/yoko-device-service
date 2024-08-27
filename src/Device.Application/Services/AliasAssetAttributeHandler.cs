using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Validation.Abstraction;
using Device.Application.Asset.Command.Model;
using Device.Application.Constant;
using Device.Application.Repository;
using Newtonsoft.Json.Linq;
using Device.ApplicationExtension.Extension;
namespace Device.Application.Service
{
    public class AliasAssetAttributeHandler : BaseAssetAttributeHandler
    {
        private readonly IDynamicValidator _dynamicValidator;
        private readonly ICache _cache;
        private readonly IReadAssetAttributeAliasRepository _readAssetAttributeAliasRepository;

        public AliasAssetAttributeHandler(
            IDynamicValidator dynamicValidator,
            IAssetUnitOfWork repository,
            IAuditLogService auditLogService,
            IDomainEventDispatcher domainEventDispatcher,
            IAssetAttributeTemplateRepository attributeTemplateRepository,
            ITenantContext tenantContext,
            ICache cache,
            IReadAssetAttributeAliasRepository readAssetAttributeAliasRepository,
            IReadAssetAttributeRepository readAssetAttributeRepository,
            IReadAssetRepository readAssetRepository,
            IReadAssetAttributeTemplateRepository readAssetAttributeTemplateRepository) : base(repository, auditLogService, domainEventDispatcher, tenantContext, readAssetAttributeRepository, readAssetRepository, readAssetAttributeTemplateRepository)
        {
            _dynamicValidator = dynamicValidator;
            _cache = cache;
            _readAssetAttributeAliasRepository = readAssetAttributeAliasRepository;
        }

        /// <param name="attribute">processing attribute</param>
        /// <param name="inputAttributes">all attributes related to asset</param>
        protected override async Task<Domain.Entity.AssetAttribute> AddAttributeAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken, bool ignoreValidation = false)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var aliasPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeAlias>();
            await _dynamicValidator.ValidateAsync(aliasPayload, cancellationToken);
            var entity = Asset.Command.AssetAttribute.Create(attribute);
            entity.CreatedUtc = attribute.CreatedUtc;
            entity.SequentialNumber = attribute.SequentialNumber;
            entity.AssetAttributeAlias = AssetAttributeAlias.Create(aliasPayload);
            if (!ignoreValidation)
            {
                await ValidateAliasAttributeAsync(entity);
            }
            return await _unitOfWork.AssetAttributes.AddEntityAsync(entity);
        }

        /// <param name="attribute">processing attribute</param>
        /// <param name="inputAttributes">all attributes related to asset</param>
        protected override async Task<Domain.Entity.AssetAttribute> UpdateAttributeAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var aliasPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeAlias>();
            await _dynamicValidator.ValidateAsync(aliasPayload, cancellationToken);
            var entity = Asset.Command.AssetAttribute.Create(attribute);
            entity.AssetAttributeAlias = AssetAttributeAlias.Create(aliasPayload);

            await ValidateAliasAttributeAsync(entity);

            var asset = await _readAssetRepository.OnlyAssetAsQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.AssetId);
            if (asset == null)
            {
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.AssetAttribute.AssetId));
            }

            if (asset.AssetTemplateId.HasValue)
            {
                var aliasMapping = _unitOfWork.Assets.AsQueryable().Where(x => x.Id == asset.Id).SelectMany(x => x.AssetAttributeAliasMappings).FirstOrDefault(x => x.Id == entity.Id);
                if (aliasMapping != null)
                {
                    aliasMapping.AliasAssetId = entity.AssetAttributeAlias.AliasAssetId;
                    aliasMapping.AliasAttributeId = entity.AssetAttributeAlias.AliasAttributeId;
                    aliasMapping.UpdatedUtc = System.DateTime.UtcNow;
                    _unitOfWork.AssetAttributes.TrackMappingEntity(aliasMapping, EntityState.Modified);

                    var rootAttributeIdHashField = CacheKey.ALIAS_REFERENCE_ID_HASH_FIELD.GetCacheKey(attribute.Id);
                    var rootAttributeIdHashKey = CacheKey.ALIAS_REFERENCE_ID_HASH_KEY.GetCacheKey(_tenantContext.ProjectId);

                    await _cache.DeleteHashByKeyAsync(rootAttributeIdHashKey, rootAttributeIdHashField);
                    return entity;
                }
            }

            var trackingEntity = await _unitOfWork.AssetAttributes.AsQueryable().Include(x => x.AssetAttributeAlias).FirstOrDefaultAsync(x => x.Id == entity.Id);
            if (trackingEntity == null)
                throw new EntityNotFoundException();

            _unitOfWork.AssetAttributes.ProcessUpdate(entity, trackingEntity);
            if (trackingEntity.AssetAttributeAlias == null)
            {
                trackingEntity.AssetAttributeAlias = entity.AssetAttributeAlias;
                _unitOfWork.AssetAttributes.TrackMappingEntity(trackingEntity.AssetAttributeAlias);
            }
            else
            {
                trackingEntity.AssetAttributeAlias.AliasAssetId = entity.AssetAttributeAlias.AliasAssetId;
                trackingEntity.AssetAttributeAlias.AliasAttributeId = entity.AssetAttributeAlias.AliasAttributeId;
                trackingEntity.AssetAttributeAlias.UpdatedUtc = System.DateTime.UtcNow;
                await _unitOfWork.AssetAttributes.UpdateEntityAsync(trackingEntity);
            }

            return trackingEntity;
        }

        private async Task ValidateAliasAttributeAsync(Domain.Entity.AssetAttribute entity)
        {
            //valid asset is deleted before save with case alias
            var aliasAsset = await _readAssetRepository.FindAsync(entity.AssetAttributeAlias.AliasAssetId ?? Guid.Empty);

            if (aliasAsset == null)
            {
                aliasAsset = _unitOfWork.Assets.UnSaveAssets.FirstOrDefault(x => x.Id == entity.AssetAttributeAlias.AliasAssetId);
            }

            var dto = GetAssetDto.Create(aliasAsset);

            if (dto == null)
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.AssetAttributeAlias.AliasAssetId));

            var attribute = dto.Attributes.FirstOrDefault(x => x.Id == entity.AssetAttributeAlias.AliasAttributeId);
            if (attribute == null)
            {
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.AssetAttributeAlias.AliasAttributeId));
            }

            if (attribute.AttributeType == AttributeTypeConstants.TYPE_COMMAND)
            {
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(Domain.Entity.AssetAttribute.AttributeType));
            }

            //Should throw validation exception when alias link to other alias which isn't map yet.
            if (attribute.AttributeType == AttributeTypeConstants.TYPE_ALIAS && attribute.DataType == null)
            {
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(Domain.Entity.AssetAttribute.AttributeType));
            }
            // validate cicle between alias
            var isCircle = await ValidateCircleAliasAsync(entity.Id, entity.AssetAttributeAlias.AliasAttributeId ?? Guid.Empty);
            if (isCircle)
            {
                throw new CircularReferenceException(MessageConstants.ASSET_ALIAS_CIRCULAR_REFERENCE);
            }
            entity.DataType = attribute.DataType;
        }

        public Task<bool> ValidateCircleAliasAsync(Guid attributeId, Guid aliasAttributeId)
        {
            return _readAssetAttributeAliasRepository.ValidateCircleAliasAsync(attributeId, aliasAttributeId);
        }

        public override bool CanApply(string attributeType)
        {
            return attributeType == AttributeTypeConstants.TYPE_ALIAS;
        }
    }

    internal class AssetAttributeAlias
    {
        public Guid AssetAttributeId { get; set; }
        public Guid AliasAssetId { get; set; }
        public Guid AliasAttributeId { get; set; }

        internal static Domain.Entity.AssetAttributeAlias Create(AssetAttributeAlias aliasPayload)
        {
            Guid? nullGuid = null;
            return new Domain.Entity.AssetAttributeAlias
            {
                AssetAttributeId = aliasPayload.AssetAttributeId,
                AliasAssetId = !IsNullOrEmptyGuid(aliasPayload.AliasAssetId) ? aliasPayload.AliasAssetId : nullGuid,
                AliasAttributeId = !IsNullOrEmptyGuid(aliasPayload.AliasAttributeId) ? aliasPayload.AliasAttributeId : nullGuid
            };
        }

        private static bool IsNullOrEmptyGuid(Guid guid)
        {
            return guid == Guid.Empty || guid == null;
        }
    }
}