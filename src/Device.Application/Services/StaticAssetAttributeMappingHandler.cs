using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.Asset;
using Device.Application.Constant;
using Device.Application.Repository;
using Newtonsoft.Json.Linq;

namespace Device.Application.Service
{
    public class StaticAssetAttributeMappingHandler : BaseAssetAttributeMappingHandler
    {
        private readonly IAssetUnitOfWork _unitOfWork;
        public StaticAssetAttributeMappingHandler(IAssetUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        protected override bool CanApply(string type)
        {
            return type == AttributeTypeConstants.TYPE_STATIC;
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
            var mappingDto = mapping == null ? null : JObject.FromObject(mapping).ToObject<AddAssetAttributeStaticMapping>();
            var value = templateAttribute.Value;
            if (mappingDto != null)
            {
                value = mappingDto.Value;
            }
            var mappingEntity = new Domain.Entity.AssetAttributeStaticMapping()
            {
                Id = mappingAttributes.ContainsKey(templateAttribute.Id) ? mappingAttributes[templateAttribute.Id] : Guid.NewGuid(),
                AssetId = asset.Id,
                AssetAttributeTemplateId = templateAttribute.Id,
                Value = value,
                SequentialNumber = templateAttribute.SequentialNumber,
            };

            if (isKeepCreatedUtc != null && isKeepCreatedUtc.Value)
            {
                mappingEntity.CreatedUtc = templateAttribute.CreatedUtc;
            }

            asset.AssetAttributeStaticMappings.Add(mappingEntity);
            _unitOfWork.AssetAttributes.TrackMappingEntity(mappingEntity);
            return Task.FromResult(mappingEntity.Id);
        }
    }

    public class AddAssetAttributeStaticMapping
    {
        public string Value { get; set; }
    }
}
