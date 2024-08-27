using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Device.Application.Asset;
using Device.Application.Constant;
using Device.Application.Repository;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace Device.Application.Service
{
    public class CommandAssetAttributeMappingHandler : BaseAssetAttributeMappingHandler
    {
        private readonly IAssetUnitOfWork _unitOfWork;
        private readonly IReadAssetAttributeRepository _readAssetAttributeRepository;
        private readonly IReadAssetRepository _readAssetRepository;

        public CommandAssetAttributeMappingHandler(IAssetUnitOfWork unitOfWork,
            IReadAssetAttributeRepository readAssetAttributeRepository,
            IReadAssetRepository readAssetRepository)
        {
            _unitOfWork = unitOfWork;
            _readAssetAttributeRepository = readAssetAttributeRepository;
            _readAssetRepository = readAssetRepository;
        }

        protected override bool CanApply(string type)
        {
            return type == AttributeTypeConstants.TYPE_COMMAND;
        }

        /// <summary>
        /// Decorate asset with template attribute.
        /// </summary>
        /// <param name="asset"> processing asset.</param>
        /// <param name="templateAttribute"> processing attribute.</param>
        /// <param name="mappingAttributes"> mapping template attribute id & new asset attribute id.</param>
        /// <param name="mapping"> the mapping.</param>
        /// <param name="isKeepCreatedUtc"> the isKeepCreatedUtc.</param>
        /// <returns>asset Id.</returns>
        protected override async Task<Guid> DecorateAssetWithTemplateAttributeAsync(
            Domain.Entity.Asset asset,
            Domain.Entity.AssetAttributeTemplate templateAttribute,
            IDictionary<Guid, Guid> mappingAttributes,
            AttributeMapping mapping,
            bool? isKeepCreatedUtc = false)
        {
            if (mapping == null)
            {
                throw new ArgumentException(nameof(mapping));
            }
            var mappingDto = JObject.FromObject(mapping).ToObject<AssetAttributeCommandMapping>();

            if (mappingDto != null)
            {
                var metricUsing = (await _readAssetAttributeRepository.AsQueryable()
                                    .Include(x => x.AssetAttributeCommand)
                                    .AnyAsync(x => x.AssetAttributeCommand.MetricKey == templateAttribute.AssetAttributeCommand.MetricKey
                                    && x.AssetAttributeCommand.DeviceId == mappingDto.DeviceId))
                                || (await _readAssetRepository.OnlyAssetAsQueryable().Include(x => x.AssetAttributeCommandMappings)
                                    .SelectMany(x => x.AssetAttributeCommandMappings)
                                    .AnyAsync(x => x.MetricKey == templateAttribute.AssetAttributeCommand.MetricKey
                                    && x.DeviceId == mappingDto.DeviceId && x.AssetId != asset.Id))
                                || _unitOfWork.Assets.UnSaveAssets.Where(x => x.AssetAttributeCommandMappings != null)
                                    .SelectMany(x => x.AssetAttributeCommandMappings)
                                    .Any(x => x.MetricKey == templateAttribute.AssetAttributeCommand.MetricKey
                                    && x.DeviceId == mappingDto.DeviceId && x.AssetId != asset.Id);
                if (metricUsing)
                {
                    mappingDto = null;
                }
            }

            var entity = new Domain.Entity.AssetAttributeCommandMapping()
            {
                Id = mappingAttributes.ContainsKey(templateAttribute.Id) ? mappingAttributes[templateAttribute.Id] : Guid.NewGuid(),
                AssetId = asset.Id,
                AssetAttributeTemplateId = templateAttribute.Id,
                DeviceId = mappingDto?.DeviceId,
                MetricKey = templateAttribute.AssetAttributeCommand.MetricKey,
                SequentialNumber = templateAttribute.SequentialNumber
            };

            if (isKeepCreatedUtc != null && isKeepCreatedUtc.Value)
            {
                entity.CreatedUtc = templateAttribute.CreatedUtc;
            }

            asset.AssetAttributeCommandMappings.Add(entity);
            _unitOfWork.AssetAttributes.TrackMappingEntity(entity);
            return entity.Id;
        }
    }

    internal class AssetAttributeCommandMapping
    {
        public string DeviceId { get; set; }
    }
}
