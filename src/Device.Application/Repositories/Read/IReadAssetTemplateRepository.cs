using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.AssetTemplate.Command.Model;

namespace Device.Application.Repository
{
    public interface IReadAssetTemplateRepository : IReadRepository<Domain.Entity.AssetTemplate, Guid>
    {
        Task RetrieveAsync(IEnumerable<Domain.Entity.AssetTemplate> templates);
        Task<GetAssetTemplateDto> GetAssetTemplateAsync(Guid assetTemplateId);
    }
}
