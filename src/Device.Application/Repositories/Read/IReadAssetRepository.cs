using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Device.Application.Asset.Command;
using Device.Application.Asset.Command.Model;
using Device.Domain.Entity;

namespace Device.Application.Repository
{
    public interface IReadAssetRepository : IReadRepository<Domain.Entity.Asset, Guid>
    {
        Task<IEnumerable<GetAssetSimpleDto>> GetAssetSimpleAsync(GetAssetByCriteria criteria, bool paging);
        Task<int> CountAsync(GetAssetByCriteria criteria);
        Task<IEnumerable<AssetHierarchy>> HierarchySearchAsync(string assetName, IEnumerable<long> tagIds);
        Task<IEnumerable<Guid>> GetAllRelatedAssetIdAsync(Guid assetId);
        Task<bool> ValidateParentExistedAsync(Guid parentAssetId);
        Task<IEnumerable<Guid>> GetExistingAssetIdsAsync(IEnumerable<Guid> ids);
        Task<AliasAttributeReference> FindTargetAttributeAsync(Guid attributeId);
        Task<bool> ValidateAssetAsync(Guid id);
        Task<IEnumerable<Guid>> GetAllRelatedChildAssetIdAsync(Guid assetId);
        Task<int> GetTotalAssetAsync();
        Task<Domain.Entity.Asset> FindFullAssetByIdAsync(Guid id);
        Task<Domain.Entity.Asset> FindByIdAsync(Guid id);
        IQueryable<Domain.Entity.Asset> AssetTemplateAsQueryable();
        IQueryable<Domain.Entity.Asset> OnlyAssetAsQueryable();
        Task<IEnumerable<GetAssetSimpleDto>> GetAssetsByTemplateIdAsync(Guid assetTemplateId);
    }
}
