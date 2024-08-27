using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.AssetTemplate.Command;

namespace Device.Application.Service.Abstraction
{
    public interface IAssetTemplateAttributeHandler
    {
        bool CanApply(string attributeType);
        Task<Domain.Entity.AssetAttributeTemplate> AddAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken);
        Task<Domain.Entity.AssetAttributeTemplate> UpdateAsync(AssetTemplateAttribute attribute, IEnumerable<AssetTemplateAttribute> inputAttributes, CancellationToken cancellationToken);
    }
}