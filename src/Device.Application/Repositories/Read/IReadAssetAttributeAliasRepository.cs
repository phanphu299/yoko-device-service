using System;
using System.Threading.Tasks;
using Device.Domain.Entity;

namespace Device.Application.Repository
{
    public interface IReadAssetAttributeAliasRepository : IReadRepository<AssetAttributeAlias, Guid>
    {
        Task<Domain.Entity.AssetAttributeAlias> GetParentIdByElementPropertyAliasId(Guid aliasAtributeId, Guid aliasAssetId);
        Task<bool> CheckExistAliasMappingAsync(Guid? aliasAtributeId, Guid aliasAssetId);
        Task<bool> ExistsAsync(Guid aliasAtributeId, Guid aliasAssetId);
        Task<bool> ValidateCircleAliasAsync(Guid attributeId, Guid aliasAttributeId);
        Task<Guid?> GetTargetAliasAttributeIdAsync(Guid aliasAttributeId);
    }
}
