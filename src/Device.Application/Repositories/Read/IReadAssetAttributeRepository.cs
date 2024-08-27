using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.Model;

namespace Device.Application.Repository
{
    public interface IReadAssetAttributeRepository : IReadRepository<Domain.Entity.AssetAttribute, Guid>
    {
        Task<IEnumerable<ValidateAssetAttribute>> QueryAssetAttributeSeriesDataAsync(IEnumerable<Guid> attributeIds);
        Task<IEnumerable<Domain.ValueObject.AssetDependency>> GetAssetAttributeDependencyAsync(Guid[] attributeIds);
    }
}
