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
using Device.ApplicationExtension.Extension;

namespace Device.Application.Service
{
    public class StaticAssetAttributeHandler : BaseAssetAttributeHandler
    {
        private readonly IDynamicValidator _dynamicValidator;
        private readonly IReadUomRepository _readUomRepository;

        public StaticAssetAttributeHandler(
            IDynamicValidator dynamicValidator,
            IAssetUnitOfWork repository,
            IAuditLogService auditLogService,
            IDomainEventDispatcher domainEventDispatcher,
            ITenantContext tenantContext,
            IReadAssetAttributeTemplateRepository readAssetAttributeTemplateRepository,
            IReadAssetAttributeRepository readAssetAttributeRepository,
            IReadUomRepository readUomRepository,
            IReadAssetRepository readAssetRepository) : base(repository, auditLogService, domainEventDispatcher, tenantContext, readAssetAttributeRepository, readAssetRepository, readAssetAttributeTemplateRepository)
        {
            _dynamicValidator = dynamicValidator;
            _readUomRepository = readUomRepository;
        }

        /// <param name="attribute">processing attribute</param>
        /// <param name="inputAttributes">all attributes related to asset</param>
        protected override async Task<Domain.Entity.AssetAttribute> AddAttributeAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken, bool ignoreValidation = false)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var entity = Asset.Command.AssetAttribute.Create(attribute);
            entity.CreatedUtc = attribute.CreatedUtc;
            entity.SequentialNumber = attribute.SequentialNumber;
            if (!ignoreValidation)
            {
                ValidateStaticAttribute(entity);
                await ValidateExistUomByIdAsync(entity);
            }

            return await _unitOfWork.AssetAttributes.AddEntityAsync(entity);
        }

        /// <param name="attribute">processing attribute</param>
        /// <param name="inputAttributes">all attributes related to asset</param>
        protected override async Task<Domain.Entity.AssetAttribute> UpdateAttributeAsync(Asset.Command.AssetAttribute attribute, IEnumerable<Asset.Command.AssetAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var entity = Asset.Command.AssetAttribute.Create(attribute);
            var asset = await _readAssetRepository.OnlyAssetAsQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.Id == entity.AssetId);

            ValidateStaticAttribute(entity);
            await ValidateExistUomByIdAsync(entity);

            if (asset == null)
            {
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.AssetAttribute.AssetId));
            }

            if (asset.AssetTemplateId.HasValue)
            {
                var staticMapping = _unitOfWork.Assets.AsQueryable().Where(x => x.Id == asset.Id).SelectMany(x => x.AssetAttributeStaticMappings).FirstOrDefault(x => x.Id == entity.Id);

                // can be the case asset create from template and then adding more attributes
                if (staticMapping != null)
                {
                    staticMapping.Value = attribute.Value;
                    staticMapping.IsOverridden = true;
                    _unitOfWork.AssetAttributes.TrackMappingEntity(staticMapping, EntityState.Modified);
                    return entity;
                }
            }

            //fallback to asset attribute instead of finding in asset template
            var trackingEntity = await _unitOfWork.AssetAttributes.AsQueryable().FirstOrDefaultAsync(x => x.Id == entity.Id);
            if (trackingEntity != null)
            {
                _unitOfWork.AssetAttributes.ProcessUpdate(entity, trackingEntity);
                return await _unitOfWork.AssetAttributes.UpdateEntityAsync(trackingEntity);
            }

            throw new EntityNotFoundException();
        }

        private void ValidateStaticAttribute(Domain.Entity.AssetAttribute attribute)
        {
            var isValueValid = ValidateValue(attribute.Value, attribute.DataType);
            if (!isValueValid)
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(Domain.Entity.AssetAttribute.Value));
        }

        public bool ValidateValue(string value, string dataType)
        {
            //can leave value empty with static attribute
            if (string.IsNullOrEmpty(value))
                return true;

            try
            {
                var isValueValid = value.CanParseResultWithDataType(dataType);
                return isValueValid;
            }
            catch (System.Exception ex)
            {
                if (ex is System.OverflowException)
                    throw new EntityInvalidException(detailCode: MessageConstants.ATTRIBUTE_VALUE_OVERFLOW);
                return false;
            }
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

        public override bool CanApply(string attributeType)
        {
            return attributeType == AttributeTypeConstants.TYPE_STATIC;
        }
    }
}