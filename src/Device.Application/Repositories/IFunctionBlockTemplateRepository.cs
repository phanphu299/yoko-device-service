using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Infrastructure.Repository.Generic;
namespace Device.Application.Repository
{
    public interface IFunctionBlockTemplateRepository : IRepository<Domain.Entity.FunctionBlockTemplate, Guid>
    {
        Task<Domain.Entity.FunctionBlockTemplate> AddEntityAsync(Domain.Entity.FunctionBlockTemplate entity);
        Task<Domain.Entity.FunctionBlockTemplate> UpdateEntityAsync(Domain.Entity.FunctionBlockTemplate entity);
        Task<bool> RemoveEntityAsync(Domain.Entity.FunctionBlockTemplate entity);
        Task RetrieveAsync(IEnumerable<Domain.Entity.FunctionBlockTemplate> templates);
    }
}