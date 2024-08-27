using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.Asset;
using Device.Application.Constant;
using Device.Application.Repository;
using Newtonsoft.Json.Linq;

namespace Device.Application.Service
{
    public class IntegrationAssetAttributeMappingHandler : BaseAssetAttributeMappingHandler
    {
        private readonly IAssetUnitOfWork _unitOfWork;

        public IntegrationAssetAttributeMappingHandler(IAssetUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        protected override bool CanApply(string type)
        {
            return type == AttributeTypeConstants.TYPE_INTEGRATION;
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
        protected override Task<Guid> DecorateAssetWithTemplateAttributeAsync(
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
            var mappingDto = JObject.FromObject(mapping).ToObject<AssetAttributeIntegrationMapping>();
            var entity = new Domain.Entity.AssetAttributeIntegrationMapping()
            {
                Id = mappingAttributes.ContainsKey(templateAttribute.Id) ? mappingAttributes[templateAttribute.Id] : Guid.NewGuid(),
                AssetAttributeTemplateId = templateAttribute.Id,
                IntegrationId = mappingDto.IntegrationId,
                DeviceId = mappingDto.DeviceId,
                AssetId = asset.Id,
                MetricKey = templateAttribute.AssetAttributeIntegration.MetricKey,
                SequentialNumber = templateAttribute.SequentialNumber
            };

            if (isKeepCreatedUtc != null && isKeepCreatedUtc.Value)
            {
                entity.CreatedUtc = templateAttribute.CreatedUtc;
            }

            asset.AssetAttributeIntegrationMappings.Add(entity);
            _unitOfWork.AssetAttributes.TrackMappingEntity(entity);
            return Task.FromResult(entity.Id);
        }
    }

    internal class AssetAttributeIntegrationMapping
    {
        public Guid TemplateAttributeId { get; set; }
        public Guid? IntegrationId { get; set; }
        public string DeviceId { get; set; }
    }
}
