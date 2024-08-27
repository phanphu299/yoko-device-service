using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using Device.Application.Asset;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public abstract class BaseAssetAttributeMappingHandler : IAttributeMappingHandler
    {
        private IAttributeMappingHandler _next;

        public void SetNextHandler(IAttributeMappingHandler next)
        {
            _next = next;
        }

        protected abstract bool CanApply(string type);

        /// <summary>
        /// Decorate asset with template attribute.
        /// </summary>
        /// <param name="asset"> processing asset.</param>
        /// <param name="templateAttribute"> processing attribute.</param>
        /// <param name="mappingAttributes"> mapping template attribute id & new asset attribute id.</param>
        /// <param name="mapping"> the mapping.</param>
        /// <param name="isKeepCreatedUtc"> the isKeepCreatedUtc.</param>
        /// <returns>asset Id.</returns>
        public Task<Guid> DecorateAssetBasedOnTemplateAsync(
            Domain.Entity.Asset asset,
            Domain.Entity.AssetAttributeTemplate templateAttribute,
            IDictionary<Guid, Guid> mappingAttributes,
            AttributeMapping mapping,
            bool? isKeepCreatedUtc = false)
        {
            if (CanApply(templateAttribute.AttributeType))
            {
                return DecorateAssetWithTemplateAttributeAsync(asset, templateAttribute, mappingAttributes, mapping, isKeepCreatedUtc);
            }
            else if (_next != null)
            {
                return _next.DecorateAssetBasedOnTemplateAsync(asset, templateAttribute, mappingAttributes, mapping, isKeepCreatedUtc);
            }
            throw new SystemNotSupportedException(detailCode: MessageConstants.ASSET_ATTRIBUTE_TYPE_INVALID);
        }

        protected abstract Task<Guid> DecorateAssetWithTemplateAttributeAsync(Domain.Entity.Asset asset, Domain.Entity.AssetAttributeTemplate templateAttribute, IDictionary<Guid, Guid> mappingAttributes, AttributeMapping mapping, bool? isKeepCreatedUtc = false);
    }
}
