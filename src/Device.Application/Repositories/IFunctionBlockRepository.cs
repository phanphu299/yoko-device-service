using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Infrastructure.Repository.Generic;

namespace Device.Application.Repository
{
    public interface IFunctionBlockRepository : IRepository<Domain.Entity.FunctionBlock, Guid>
    {
        Task<Domain.Entity.FunctionBlock> AddEntityWithRelationAsync(Domain.Entity.FunctionBlock block);
        Task<Domain.Entity.FunctionBlock> UpdateEntityWithRelationAsync(Guid id, Domain.Entity.FunctionBlock requestBlock, IEnumerable<Guid> bindingIds);
        Task RetrieveAsync(IEnumerable<Domain.Entity.FunctionBlock> functionBlocks);
    }
}