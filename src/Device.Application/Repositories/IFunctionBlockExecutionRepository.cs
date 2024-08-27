using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Infrastructure.Repository.Generic;
using Device.Domain.Entity;
namespace Device.Application.Repository
{
    public interface IFunctionBlockExecutionRepository : IRepository<FunctionBlockExecution, Guid>
    {
        Task UpsertBlockNodeMappingAsync(IEnumerable<FunctionBlockNodeMapping> mappings, Guid executionId);
        Task RemoveMappingAsync(IEnumerable<FunctionBlockNodeMapping> mappings);
        Task RetrieveAsync(IEnumerable<Domain.Entity.FunctionBlockExecution> functionBlockExecutions);
    }
}