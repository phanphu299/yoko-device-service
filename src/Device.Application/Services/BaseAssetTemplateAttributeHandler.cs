using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using Device.Application.AssetTemplate.Command;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using System.Collections.Generic;

namespace Device.Application.Service
{
    public abstract class BaseAssetTemplateAttributeHandler : IAssetTemplateAttributeHandler
    {
        private IAssetTemplateAttributeHandler _next;
        public void SetNextHandler(IAssetTemplateAttributeHandler next)
        {
            _next = next;
        }
        public Task<Domain.Entity.AssetAttributeTemplate> AddAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            if (CanApply(attribute.AttributeType))
            {
                return AddAttributeAsync(attribute, inputAttributes, cancellationToken);
            }
            else if (_next != null)
            {
                return _next.AddAsync(attribute, inputAttributes, cancellationToken);
            }
            throw new SystemNotSupportedException(detailCode: MessageConstants.ASSET_TEMPLATE_ATTRIBUTE_TYPE_INVALID);
        }
        public Task<Domain.Entity.AssetAttributeTemplate> UpdateAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken)
        {
            if (CanApply(attribute.AttributeType))
            {
                return UpdateAttributeAsync(attribute, inputAttributes, cancellationToken);
            }
            else if (_next != null)
            {
                return _next.UpdateAsync(attribute, inputAttributes, cancellationToken);
            }
            throw new SystemNotSupportedException(detailCode: MessageConstants.ASSET_TEMPLATE_ATTRIBUTE_TYPE_INVALID);
        }
        protected abstract Task<Domain.Entity.AssetAttributeTemplate> AddAttributeAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken);

        public abstract bool CanApply(string attributeType);

        protected abstract Task<Domain.Entity.AssetAttributeTemplate> UpdateAttributeAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken);
    }
}