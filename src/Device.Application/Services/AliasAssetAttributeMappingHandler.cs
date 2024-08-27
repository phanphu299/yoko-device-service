using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.Asset;
using Device.Application.Constant;
using Device.Application.Repository;
using Newtonsoft.Json.Linq;

namespace Device.Application.Service
{
    public class AliasAssetAttributeMappingHandler : BaseAssetAttributeMappingHandler
    {
        private readonly IAssetUnitOfWork _unitOfWork;

        public AliasAssetAttributeMappingHandler(IAssetUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        protected override bool CanApply(string type)
        {
            return type == AttributeTypeConstants.TYPE_ALIAS;
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
            var mappingDto = mapping == null ? null : JObject.FromObject(mapping).ToObject<AddAssetAttributeAliasMapping>();
            var mappingEntity = new Domain.Entity.AssetAttributeAliasMapping()
            {
                Id = mappingAttributes.ContainsKey(templateAttribute.Id) ? mappingAttributes[templateAttribute.Id] : Guid.NewGuid(),
                AssetId = asset.Id,
                AssetAttributeTemplateId = templateAttribute.Id,
                AliasAssetId = mappingDto?.AliasAssetId,
                AliasAttributeId = mappingDto?.AliasAttributeId
            };

            if (isKeepCreatedUtc != null && isKeepCreatedUtc.Value)
            {
                mappingEntity.CreatedUtc = templateAttribute.CreatedUtc;
            }
            asset.AssetAttributeAliasMappings.Add(mappingEntity);
            _unitOfWork.AssetAttributes.TrackMappingEntity(mappingEntity);
            return Task.FromResult(mappingEntity.Id);
        }
    }

    public class AddAssetAttributeAliasMapping : Dictionary<string, object>
    {
        public string Name { get; set; }

        public Guid? AliasAssetId
        {
            get
            {
                if (this.ContainsKey("aliasAssetId") && this["aliasAssetId"] != null)
                {
                    return Guid.Parse(this["aliasAssetId"] as string);
                }
                if (this.ContainsKey("AliasAssetId") && this["AliasAssetId"] != null)
                {
                    return Guid.Parse(this["AliasAssetId"] as string);
                }
                return null;
            }
        }

        public Guid? AliasAttributeId
        {
            get
            {
                if (this.ContainsKey("aliasAttributeId") && this["aliasAttributeId"] != null)
                {
                    return Guid.Parse(this["aliasAttributeId"] as string);
                }
                if (this.ContainsKey("AliasAttributeId") && this["AliasAttributeId"] != null)
                {
                    return Guid.Parse(this["AliasAttributeId"] as string);
                }
                return null;
            }
        }
    }
}
