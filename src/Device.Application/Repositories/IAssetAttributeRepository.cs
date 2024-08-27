using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using AHI.Infrastructure.Repository.Generic;
using Device.Application.Model;

namespace Device.Application.Repository
{
    public interface IAssetAttributeRepository : IRepository<Domain.Entity.AssetAttribute, Guid>
    {
        Task<Domain.Entity.AssetAttribute> AddEntityAsync(Domain.Entity.AssetAttribute attribute);

        Task<Domain.Entity.AssetAttribute> UpdateEntityAsync(Domain.Entity.AssetAttribute attribute);

        void ProcessUpdate(Domain.Entity.AssetAttribute requestObject, Domain.Entity.AssetAttribute targetObject);

        void TrackMappingEntity<TEntity>(TEntity entity, EntityState entityState = EntityState.Added) where TEntity : class;
        Task<IEnumerable<ValidateAssetAttribute>> QueryAssetAttributeSeriesDataAsync(IEnumerable<Guid> attributeIds);

        Task AddAssetRuntimeAttributeTriggersAsync(IEnumerable<Domain.Entity.AssetAttributeRuntimeTrigger> entities);

        Task RemoveAssetRuntimeAttributeTriggersAsync(Guid assetAttributeId);

        LocalView<Domain.Entity.AssetAttribute> UnSaveAttributes { get; }

        LocalView<Domain.Entity.AssetAttributeRuntime> UnSaveAttributeRuntimes { get; }

        Task<Guid> TrackCommandHistoryAsync(Guid assetAttributeId, string deviceId, string metricKey, Guid version, string value);

        Task<IEnumerable<Domain.ValueObject.AssetDependency>> GetAssetAttributeDependencyAsync(Guid[] attributeIds);
    }
}
