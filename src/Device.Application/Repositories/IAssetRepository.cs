using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using AHI.Infrastructure.Repository.Generic;
using Device.Domain.Entity;
using System.Linq;

namespace Device.Application.Repository
{
    public interface IAssetRepository : IRepository<Domain.Entity.Asset, Guid>
    {
        Task<IEnumerable<AssetHierarchy>> HierarchySearchAsync(string assetName, IEnumerable<long> tagIds);
        Task<Domain.Entity.Asset> AddEntityAsync(Domain.Entity.Asset entity);
        Task<Domain.Entity.Asset> UpdateEntityAsync(Domain.Entity.Asset entity);
        Task<bool> RemoveEntityAsync(Domain.Entity.Asset entity);
        Task<IEnumerable<Guid>> GetAllRelatedAssetIdAsync(Guid assetId);
        Task<bool> ValidateParentExistedAsync(Guid parentAssetId);
        Task<IEnumerable<AssetPath>> GetPathsAsync(IEnumerable<Guid> assetIds);
        Task<IEnumerable<Guid>> GetExistingAssetIdsAsync(IEnumerable<Guid> ids);
        LocalView<Domain.Entity.Asset> UnSaveAssets { get; }
        Task<AliasAttributeReference> FindTargetAttributeAsync(Guid attributeId);
        Task<bool> ValidateAssetAsync(Guid id);
        Task<IEnumerable<Guid>> GetAllRelatedChildAssetIdAsync(Guid assetId);
        Task<int> GetTotalAssetAsync();
        Task UpdateAssetPathAsync(Guid id);
        Task RetrieveAsync(IEnumerable<Domain.Entity.Asset> assets);
        Task<Domain.Entity.Asset> FindFullAssetByIdAsync(Guid id);
        Task<Domain.Entity.Asset> FindSnapshotAsync(Guid id);
        IQueryable<Domain.Entity.Asset> OnlyAssetAsQueryable();
    }
}
