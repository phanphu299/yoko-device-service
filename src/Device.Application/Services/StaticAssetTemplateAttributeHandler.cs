using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.Validation.Abstraction;
using Device.Application.AssetTemplate.Command;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.ApplicationExtension.Extension;

namespace Device.Application.Service
{
    public class StaticAssetTemplateAttributeHandler : BaseAssetTemplateAttributeHandler
    {
        private readonly IDynamicValidator _dynamicValidator;
        private readonly IReadAssetRepository _readAssetRepository;
        private readonly IReadUomRepository _readUomRepository;
        private readonly IAssetTemplateUnitOfWork _unitOfWork;

        public StaticAssetTemplateAttributeHandler(
            IDynamicValidator dynamicValidator,
            IReadAssetRepository readAssetRepository,
            IReadUomRepository readUomRepository,
            IAssetTemplateUnitOfWork unitOfWork)
        {
            _dynamicValidator = dynamicValidator;
            _readAssetRepository = readAssetRepository;
            _readUomRepository = readUomRepository;
            _unitOfWork = unitOfWork;
        }

        protected override async Task<Domain.Entity.AssetAttributeTemplate> AddAttributeAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var entity = AssetTemplateAttribute.Create(attribute);
            entity.AssetTemplateId = attribute.AssetTemplate.Id;
            entity.SequentialNumber = attribute.SequentialNumber;
            ValidateStaticAttribute(entity);
            await ValidateExistUomByIdAsync(entity);
            // with mapping asset
            var assetIds = await _readAssetRepository
                            .AssetTemplateAsQueryable()
                            .Where(x => x.AssetTemplateId == entity.AssetTemplateId)
                            .Select(x => x.Id)
                            .ToListAsync();
            var assetAttributeStaticMappings = new List<Domain.Entity.AssetAttributeStaticMapping>();

            foreach (var assetId in assetIds)
            {
                var staticMapping = new Domain.Entity.AssetAttributeStaticMapping
                {
                    AssetId = assetId,
                    AssetAttributeTemplateId = entity.Id,
                    Value = entity.Value
                };
                assetAttributeStaticMappings.Add(staticMapping);

            }
            entity.AssetAttributeStaticMappings = assetAttributeStaticMappings;

            return await _unitOfWork.Attributes.AddEntityAsync(entity);
        }

        protected override async Task<Domain.Entity.AssetAttributeTemplate> UpdateAttributeAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var entity = await _unitOfWork.Attributes.AsQueryable().Where(x => x.Id == attribute.Id).FirstAsync();
            entity.Name = attribute.Name;
            entity.Value = attribute.Value;
            entity.UomId = attribute.UomId;
            entity.DataType = attribute.DataType;
            entity.ThousandSeparator = attribute.ThousandSeparator;
            entity.DecimalPlace = attribute.DecimalPlace;

            ValidateStaticAttribute(entity);
            await ValidateExistUomByIdAsync(entity);

            //TODO why we update mapping that is not related to entity? Does it send the update query to DB?
            var attributeMappings = await _unitOfWork.Assets.OnlyAssetAsQueryable()
                                    .Where(x => x.AssetTemplateId == entity.AssetTemplateId)
                                    .SelectMany(x => x.AssetAttributeStaticMappings)
                                    .ToListAsync();
            foreach (var mapping in attributeMappings.Where(x => x.AssetAttributeTemplateId == attribute.Id))
            {
                // update the default value
                if (!mapping.IsOverridden)
                {
                    mapping.Value = attribute.Value;
                }
            }

            return await _unitOfWork.Attributes.UpdateEntityAsync(entity);
        }

        private void ValidateStaticAttribute(Domain.Entity.AssetAttributeTemplate attribute)
        {
            var isValueValid = ValidateValue(attribute.Value, attribute.DataType);
            if (!isValueValid)
                throw ValidationExceptionHelper.GenerateInvalidValidation(nameof(Domain.Entity.AssetAttributeTemplate.Value));
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

        public override bool CanApply(string attributeType)
        {
            return attributeType == AttributeTypeConstants.TYPE_STATIC;
        }

        private async Task ValidateExistUomByIdAsync(Domain.Entity.AssetAttributeTemplate attribute)
        {
            if (attribute.UomId.HasValue)
            {
                var uom = await _readUomRepository.AsQueryable().AsNoTracking().AnyAsync(x => x.Id == attribute.UomId);
                if (!uom)
                    throw EntityValidationExceptionHelper.GenerateException(nameof(attribute.UomId), ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
            }
        }
    }
}