using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.AssetTemplate.Command;
using Device.Application.Constant;
using Device.Application.Repository;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using AHI.Infrastructure.Validation.Abstraction;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.Exception.Helper;

namespace Device.Application.Service
{
    public class IntegrationAssetTemplateAttributeHandler : BaseAssetTemplateAttributeHandler
    {
        private readonly IDynamicValidator _dynamicValidator;
        private readonly IAssetTemplateUnitOfWork _unitOfWork;
        private readonly IReadUomRepository _readUomRepository;
        private readonly IReadAssetTemplateRepository _readAssetTemplateRepository;
        private readonly IReadAssetRepository _readAssetRepository;

        public IntegrationAssetTemplateAttributeHandler(
            IDynamicValidator dynamicValidator,
            IAssetTemplateUnitOfWork unitOfWork,
             IReadUomRepository readUomRepository,
            IReadAssetTemplateRepository readAssetTemplateRepository,
            IReadAssetRepository readAssetRepository)
        {
            _dynamicValidator = dynamicValidator;
            _unitOfWork = unitOfWork;
            _readUomRepository = readUomRepository;
            _readAssetTemplateRepository = readAssetTemplateRepository;
            _readAssetRepository = readAssetRepository;
        }

        protected override async Task<Domain.Entity.AssetAttributeTemplate> AddAttributeAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var integrationPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeIntegrationTemplate>();
            await _dynamicValidator.ValidateAsync(integrationPayload, cancellationToken);
            var entity = AssetTemplateAttribute.Create(attribute);

            await ValidateExistUomByIdAsync(entity);

            entity.AssetTemplateId = attribute.AssetTemplate.Id;
            entity.SequentialNumber = attribute.SequentialNumber;
            entity.AssetAttributeIntegration = AssetAttributeIntegrationTemplate.Create(integrationPayload);

            // with mapping asset
            var assets = await _readAssetRepository
                            .OnlyAssetAsQueryable()
                            .Include(x => x.AssetAttributeIntegrationMappings)
                            .Where(x => x.AssetTemplateId == entity.AssetTemplateId)
                            .ToListAsync();
            var assetAttributeIntegrationMappings = new List<Domain.Entity.AssetAttributeIntegrationMapping>();

            // az: https://dev.azure.com/ThanhTrungBui/yokogawa-ppm/_workitems/edit/14692
            // find the asset template which has the same deviceTemplateId
            var assetTemplateIntegrationAttribute = await _readAssetTemplateRepository.AsQueryable().Where(x => x.Id == entity.AssetTemplateId && x.Attributes.Any(att => att.AssetAttributeIntegration.IntegrationId == entity.AssetAttributeIntegration.IntegrationId && att.AssetAttributeIntegration.DeviceId == entity.AssetAttributeIntegration.DeviceId)).SelectMany(x => x.Attributes.Select(a => a.AssetAttributeIntegration)).FirstOrDefaultAsync();

            foreach (var asset in assets)
            {
                var assetMapping = asset.AssetAttributeIntegrationMappings.FirstOrDefault(x => x.AssetAttributeTemplateId == assetTemplateIntegrationAttribute?.AssetAttributeTemplateId);
                var integrationMapping = new Domain.Entity.AssetAttributeIntegrationMapping
                {
                    AssetId = asset.Id,
                    AssetAttributeTemplateId = entity.Id,
                    IntegrationId = assetMapping?.IntegrationId,
                    DeviceId = assetMapping?.DeviceId,
                    MetricKey = entity.AssetAttributeIntegration.MetricKey

                };
                assetAttributeIntegrationMappings.Add(integrationMapping);
            }

            entity.AssetAttributeIntegrationMappings = assetAttributeIntegrationMappings;
            return await _unitOfWork.Attributes.AddEntityAsync(entity);
        }

        protected override async Task<Domain.Entity.AssetAttributeTemplate> UpdateAttributeAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var integrationPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeIntegrationTemplate>();
            await _dynamicValidator.ValidateAsync(integrationPayload, cancellationToken);

            var entity = await _unitOfWork.Attributes.AsQueryable().Include(x => x.AssetAttributeIntegration).FirstAsync(x => x.Id == attribute.Id);
            entity.Name = attribute.Name;
            entity.UomId = attribute.UomId;
            entity.DataType = attribute.DataType;
            entity.AssetAttributeIntegration.MetricKey = integrationPayload.MetricKey;
            entity.AssetAttributeIntegration.IntegrationMarkupName = integrationPayload.IntegrationMarkupName;
            entity.AssetAttributeIntegration.DeviceMarkupName = integrationPayload.DeviceMarkupName;
            entity.ThousandSeparator = attribute.ThousandSeparator;
            entity.DecimalPlace = attribute.DecimalPlace;

            await ValidateExistUomByIdAsync(entity);

            var attributeMappings = await _readAssetRepository.OnlyAssetAsQueryable().Where(x => x.AssetTemplateId == entity.AssetTemplateId).SelectMany(x => x.AssetAttributeIntegrationMappings).ToListAsync();
            foreach (var mapping in attributeMappings.Where(x => x.AssetAttributeTemplateId == attribute.Id))
            {
                //update metric key regardless to the attribute mapping
                mapping.MetricKey = integrationPayload.MetricKey;

                if (integrationPayload.IntegrationId != entity.AssetAttributeIntegration.IntegrationId)
                {
                    mapping.IntegrationId = null;
                    mapping.DeviceId = null;
                    mapping.MetricKey = string.Empty;
                }
                else if (integrationPayload.DeviceId != entity.AssetAttributeIntegration.DeviceId)
                {
                    mapping.DeviceId = null;
                    mapping.MetricKey = string.Empty;
                }
            }

            entity.AssetAttributeIntegration.DeviceId = integrationPayload.DeviceId;
            entity.AssetAttributeIntegration.IntegrationId = integrationPayload.IntegrationId;

            return await _unitOfWork.Attributes.UpdateEntityAsync(entity);
        }

        public override bool CanApply(string attributeType)
        {
            return attributeType == AttributeTypeConstants.TYPE_INTEGRATION;
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

    internal class AssetAttributeIntegrationTemplate
    {
        public string IntegrationMarkupName { get; set; }
        public Guid IntegrationId { get; set; }
        public string DeviceMarkupName { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        internal static Domain.Entity.AssetAttributeTemplateIntegration Create(AssetAttributeIntegrationTemplate integrationPayload)
        {
            return new Domain.Entity.AssetAttributeTemplateIntegration()
            {
                IntegrationId = integrationPayload.IntegrationId,
                DeviceId = integrationPayload.DeviceId,
                MetricKey = integrationPayload.MetricKey,
                IntegrationMarkupName = integrationPayload.IntegrationMarkupName,
                DeviceMarkupName = integrationPayload.DeviceMarkupName
            };
        }
    }
}