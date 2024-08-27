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
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.Exception;
namespace Device.Application.Service
{
    public class DynamicAssetTemplateAttributeHandler : BaseAssetTemplateAttributeHandler
    {
        private readonly IDynamicValidator _dynamicValidator;
        private readonly IAssetTemplateUnitOfWork _unitOfWork;
        private readonly IReadUomRepository _readUomRepository;
        private readonly IReadAssetRepository _readAssetRepository;
        private readonly IReadAssetAttributeTemplateRepository _readAssetAttributeTemplateRepository;
        private readonly IReadDeviceTemplateRepository _readDeviceTemplateRepository;
        public DynamicAssetTemplateAttributeHandler(
            IDynamicValidator dynamicValidator,
            IAssetTemplateUnitOfWork unitOfWork,
            IReadUomRepository readUomRepository,
            IReadAssetRepository readAssetRepository,
            IReadAssetAttributeTemplateRepository readAssetAttributeTemplateRepository,
            IReadDeviceTemplateRepository readDeviceTemplateRepository)
        {
            _dynamicValidator = dynamicValidator;
            _unitOfWork = unitOfWork;
            _readUomRepository = readUomRepository;
            _readAssetRepository = readAssetRepository;
            _readAssetAttributeTemplateRepository = readAssetAttributeTemplateRepository;
            _readDeviceTemplateRepository = readDeviceTemplateRepository;
        }

        protected override async Task<Domain.Entity.AssetAttributeTemplate> AddAttributeAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var dynamicPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeDynamicTemplate>();
            await _dynamicValidator.ValidateAsync(dynamicPayload, cancellationToken);
            var entity = AssetTemplateAttribute.Create(attribute);
            entity.AssetTemplateId = attribute.AssetTemplate.Id;
            entity.SequentialNumber = attribute.SequentialNumber;
            entity.AssetAttributeDynamic = AssetAttributeDynamicTemplate.Create(dynamicPayload);
            await ValidateDynamicAttributeAsync(entity);

            // with mapping asset
            //TODO: why we only need assetId, but we include AssetAttributeDynamicMappings and AssetAttributeDynamicTemplate?
            var assets = await _readAssetRepository.OnlyAssetAsQueryable()
                                        .Include(x => x.AssetAttributeDynamicMappings)
                                            .ThenInclude(x => x.AssetAttributeDynamicTemplate)
                                        .Where(x => x.AssetTemplateId == entity.AssetTemplateId)
                                        .ToListAsync();
            var assetAttributeDynamicMappings = new List<Domain.Entity.AssetAttributeDynamicMapping>();

            // az: https://dev.azure.com/ThanhTrungBui/yokogawa-ppm/_workitems/edit/14692
            // find the asset template which has the same deviceTemplateId
            // updated: https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/7773: need to find the same markup deviceId
            var deviceTemplateId = entity.AssetAttributeDynamic.DeviceTemplateId;
            var markupName = entity.AssetAttributeDynamic.MarkupName;
            var assetTemplateDynamicAttribute = await _readAssetAttributeTemplateRepository.AsQueryable().Include(x => x.AssetAttributeDynamicMappings).Where(x => x.AssetTemplateId == entity.AssetTemplateId && x.AssetAttributeDynamic.DeviceTemplateId == deviceTemplateId && x.AssetAttributeDynamic.MarkupName == markupName).FirstOrDefaultAsync();

            foreach (var asset in assets)
            {
                var assetMapping = assetTemplateDynamicAttribute?.AssetAttributeDynamicMappings.FirstOrDefault();
                // find the proper deviceId
                string deviceId = assetMapping?.DeviceId;
                var dynamicMapping = new Domain.Entity.AssetAttributeDynamicMapping
                {
                    AssetId = asset.Id,
                    AssetAttributeTemplateId = entity.Id,
                    DeviceId = deviceId,
                    MetricKey = entity.AssetAttributeDynamic.MetricKey
                };
                assetAttributeDynamicMappings.Add(dynamicMapping);
            }
            entity.AssetAttributeDynamicMappings = assetAttributeDynamicMappings;

            return await _unitOfWork.Attributes.AddEntityAsync(entity);
        }

        protected override async Task<Domain.Entity.AssetAttributeTemplate> UpdateAttributeAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            await _dynamicValidator.ValidateAsync(attribute, cancellationToken);
            var dynamicPayload = JObject.FromObject(attribute.Payload).ToObject<AssetAttributeDynamicTemplate>();
            await _dynamicValidator.ValidateAsync(dynamicPayload, cancellationToken);

            var entity = await _unitOfWork.Attributes.AsQueryable().Include(x => x.AssetAttributeDynamic).FirstAsync(x => x.Id == attribute.Id);
            entity.Name = attribute.Name;
            entity.UomId = attribute.UomId;
            entity.ThousandSeparator = attribute.ThousandSeparator;
            entity.DecimalPlace = attribute.DecimalPlace;
            // fix issue: https://dev.azure.com/ThanhTrungBui/yokogawa-ppm/_workitems/edit/16309
            var currentDeviceTemplateId = entity.AssetAttributeDynamic.DeviceTemplateId;
            entity.AssetAttributeDynamic.DeviceTemplateId = dynamicPayload.DeviceTemplateId;
            entity.AssetAttributeDynamic.MetricKey = dynamicPayload.MetricKey;
            entity.AssetAttributeDynamic.MarkupName = dynamicPayload.MarkupName;
            await ValidateDynamicAttributeAsync(entity);
            var attributeMappings = await _readAssetRepository.OnlyAssetAsQueryable()
                                                            .Include(x => x.AssetAttributeDynamicMappings).ThenInclude(x => x.AssetAttributeDynamicTemplate)
                                                            .Where(x => x.AssetTemplateId == entity.AssetTemplateId)
                                                            .SelectMany(x => x.AssetAttributeDynamicMappings)
                                                            .ToListAsync();
            foreach (var mapping in attributeMappings.Where(x => x.AssetAttributeTemplateId == attribute.Id))
            {
                mapping.MetricKey = dynamicPayload.MetricKey;
                if (dynamicPayload.DeviceTemplateId != currentDeviceTemplateId)
                {
                    // if the markup already exist (and has valid device Id) in any other attribute mapping, update this mapping with the corresponding device Id
                    var currentDeviceIdWithSameMarkup = attributeMappings.Where(x => x.AssetAttributeTemplateId != attribute.Id
                                                                                  && x.AssetAttributeDynamicTemplate.MarkupName == dynamicPayload.MarkupName
                                                                                  && x.DeviceId != null)
                                                                         .FirstOrDefault()?.DeviceId;
                    mapping.DeviceId = currentDeviceIdWithSameMarkup; //TODO: #92083 Is this command saving data to DB?
                    // With device id set to null, does not need to set metric key to empty, should keep it updated according to asset attribute template
                }
            }

            return await _unitOfWork.Attributes.UpdateEntityAsync(entity);
        }

        private async Task ValidateDynamicAttributeAsync(Domain.Entity.AssetAttributeTemplate attribute)
        {
            var deviceTemplate = await _readDeviceTemplateRepository.AsQueryable().AsNoTracking().Include(x => x.Payloads).ThenInclude(x => x.Details).FirstOrDefaultAsync(x => x.Id == attribute.AssetAttributeDynamic.DeviceTemplateId);
            if (deviceTemplate == null)
                throw ValidationExceptionHelper.GenerateNotFoundValidation(nameof(Domain.Entity.AssetAttributeDynamicTemplate.DeviceTemplateId));

            if (attribute.UomId.HasValue)
            {
                var uom = await _readUomRepository.AsQueryable().AsNoTracking().AnyAsync(x => x.Id == attribute.UomId);
                if (!uom)
                    throw EntityValidationExceptionHelper.GenerateException(nameof(attribute.UomId), ExceptionErrorCode.DetailCode.ERROR_ENTITY_NOT_FOUND_SOME_ITEMS_DELETED);
            }

            var metricFieldName = nameof(Domain.Entity.AssetAttributeDynamicTemplate.MetricKey);
            if (deviceTemplate.Payloads == null || !deviceTemplate.Payloads.Any())
                throw ValidationExceptionHelper.GenerateNotFoundValidation(metricFieldName);

            var details = deviceTemplate.Payloads.SelectMany(x => x.Details);
            if (details == null || !details.Any())
                throw ValidationExceptionHelper.GenerateNotFoundValidation(metricFieldName);

            // set the dataType of this attribute base on data type of device metric
            var templateDetail = details.FirstOrDefault(x => x.Key == attribute.AssetAttributeDynamic.MetricKey);
            if (templateDetail == null)
                throw ValidationExceptionHelper.GenerateNotFoundValidation(metricFieldName);
            attribute.DataType = templateDetail.DataType;
        }
        public override bool CanApply(string attributeType)
        {
            return attributeType == AttributeTypeConstants.TYPE_DYNAMIC;
        }
    }
    internal class AssetAttributeDynamicTemplate
    {
        public Guid DeviceTemplateId { get; set; }
        public string MarkupName { get; set; }
        public string MetricKey { get; set; }

        internal static Domain.Entity.AssetAttributeDynamicTemplate Create(AssetAttributeDynamicTemplate dynamicPayload)
        {
            return new Domain.Entity.AssetAttributeDynamicTemplate()
            {
                DeviceTemplateId = dynamicPayload.DeviceTemplateId,
                MarkupName = dynamicPayload.MarkupName,
                MetricKey = dynamicPayload.MetricKey
            };
        }
    }
}