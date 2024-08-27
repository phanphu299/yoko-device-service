using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.Asset;

namespace Device.Application.Service.Abstraction
{
    public interface IAttributeMappingHandler
    {
        Task<Guid> DecorateAssetBasedOnTemplateAsync(Domain.Entity.Asset asset, Domain.Entity.AssetAttributeTemplate templateAttribute, IDictionary<Guid, Guid> mappingAttributes, AttributeMapping mapping, bool? isKeepCreatedUtc = false);
    }
}
