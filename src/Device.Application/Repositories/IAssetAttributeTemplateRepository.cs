using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using AHI.Infrastructure.Repository.Generic;
using System.Collections.Generic;

namespace Device.Application.Repository
{
    public interface IAssetAttributeTemplateRepository : IRepository<Domain.Entity.AssetAttributeTemplate, Guid>
    {
        Task<Domain.Entity.AssetAttributeTemplate> AddEntityAsync(Domain.Entity.AssetAttributeTemplate entity);
        Task<Domain.Entity.AssetAttributeTemplate> UpdateEntityAsync(Domain.Entity.AssetAttributeTemplate entity);
        void ProcessUpdate(Domain.Entity.AssetAttributeTemplate requestObject, Domain.Entity.AssetAttributeTemplate targetObject);
        [Obsolete("This method need to be refactored")]
        Task ValidateRemoveAttributeAsync(Guid attributeId);
        Task<bool> RemoveEntityAsync(Guid id, IEnumerable<Guid> deletedAttributeIds);
        Task<IEnumerable<Guid>> GetAssetAttributeIdsAsync(IEnumerable<Guid> assetAttributeTemplateId);
        LocalView<Domain.Entity.AssetAttributeTemplate> UnSaveAttributes { get; }
        Task<IEnumerable<Domain.Entity.AssetAttributeTemplate>> GetDependenciesInsideTemplateAsync(Guid attributeId);
    }
}
