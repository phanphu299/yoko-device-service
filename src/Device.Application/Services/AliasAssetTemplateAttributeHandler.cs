using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AHI.Infrastructure.Validation.Abstraction;
using Device.Application.AssetTemplate.Command;
using Device.Application.Constant;
using Device.Application.Repository;
using Device.Domain.Entity;
using System;

namespace Device.Application.Service
{
    public class AliasAssetTemplateAttributeHandler : BaseAssetTemplateAttributeHandler
    {
        private readonly IDynamicValidator _dynamicValidator;
        private readonly IAssetTemplateUnitOfWork _unitOfWork;
        private readonly IReadAssetRepository _readAssetRepository;

        public AliasAssetTemplateAttributeHandler(
            IDynamicValidator dynamicValidator,
            IAssetTemplateUnitOfWork unitOfWork,
            IReadAssetRepository readAssetRepository)
        {
            _dynamicValidator = dynamicValidator;
            _unitOfWork = unitOfWork;
            _readAssetRepository = readAssetRepository;
        }

        protected override async Task<Domain.Entity.AssetAttributeTemplate> AddAttributeAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var entity = AssetTemplateAttribute.Create(attribute);
            entity.AssetTemplateId = attribute.AssetTemplate.Id;
            entity.SequentialNumber = attribute.SequentialNumber;

            // with mapping asset
            List<Guid> assetIds = await _readAssetRepository
                            .OnlyAssetAsQueryable()
                            .Where(x => x.AssetTemplateId == entity.AssetTemplateId)
                            .Select(x => x.Id)
                            .ToListAsync();
            var assetAttributeAliasMappings = new List<AssetAttributeAliasMapping>();

            foreach (Guid assetId in assetIds)
            {
                var aliasMapping = new AssetAttributeAliasMapping
                {
                    AssetId = assetId,
                    AssetAttributeTemplateId = entity.Id
                };
                assetAttributeAliasMappings.Add(aliasMapping);
            }

            entity.AssetAttributeAliasMappings = assetAttributeAliasMappings;
            return await _unitOfWork.Attributes.AddEntityAsync(entity);
        }

        protected override async Task<Domain.Entity.AssetAttributeTemplate> UpdateAttributeAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var entity = await _unitOfWork.Attributes.AsQueryable().FirstAsync(x => x.Id == attribute.Id);
            entity.Name = attribute.Name;
            return await _unitOfWork.Attributes.UpdateEntityAsync(entity);
        }

        public override bool CanApply(string attributeType)
        {
            return attributeType == AttributeTypeConstants.TYPE_ALIAS;
        }
    }

    internal class AssetAttributeAliasTemplate
    {
        public string Name { get; set; }
    }
}