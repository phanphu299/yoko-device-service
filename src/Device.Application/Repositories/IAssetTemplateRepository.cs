using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Infrastructure.Repository.Generic;
using Device.Application.AssetTemplate.Command;
using Device.Application.AssetTemplate.Command.Model;

namespace Device.Application.Repository
{

    public interface IAssetTemplateRepository : IRepository<Domain.Entity.AssetTemplate, Guid>
    {
        Task<Domain.Entity.AssetTemplate> AddEntityAsync(Domain.Entity.AssetTemplate entity);
        Task<Domain.Entity.AssetTemplate> UpdateEntityAsync(Domain.Entity.AssetTemplate entity);
        //  Task ReloadAsync(Domain.Entity.AssetTemplate entity);
        Task<bool> RemoveEntityAsync(Domain.Entity.AssetTemplate entity);
        Task RetrieveAsync(IEnumerable<Domain.Entity.AssetTemplate> templates);
        Task<GetAssetTemplateDto> GetAssetTemplateAsync(Guid assetTemplateId);
    }
}
