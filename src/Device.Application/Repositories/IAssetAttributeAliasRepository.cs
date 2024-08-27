using System;
using System.Threading.Tasks;
using AHI.Infrastructure.Repository.Generic;
using Device.Domain.Entity;

namespace Device.Application.Repository
{
    public interface IAssetAttributeAliasRepository : IRepository<Domain.Entity.AssetAttributeAlias, Guid>
    {
        Task<Domain.Entity.AssetAttributeAlias> GetParentIdByElementPropertyAliasId(Guid aliasAtributeId, Guid aliasAssetId);
        Task<bool> CheckExistAliasMappingAsync(Guid? aliasAtributeId, Guid aliasAssetId);
        Task<bool> ExistsAsync(Guid aliasAtributeId, Guid aliasAssetId);
        Task<bool> ValidateCircleAliasAsync(Guid attributeId, Guid aliasAttributeId);
        Task<Domain.Entity.AssetAttributeAlias> UpdateEntityAsync(Domain.Entity.AssetAttributeAlias entity);
        Task<Guid?> GetTargetAliasAttributeIdAsync(Guid aliasAttributeId);
    }
}
