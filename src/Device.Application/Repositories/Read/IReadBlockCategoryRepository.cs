using System;
using Device.Domain.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Device.Application.Repository
{
    public interface IReadBlockCategoryRepository : IReadRepository<Domain.Entity.FunctionBlockCategory, Guid>
    {
        Task<IEnumerable<FunctionBlockCategoryPath>> GetPathsAsync(Guid categoryId);
        Task<IEnumerable<FunctionBlockCategoryHierarchy>> HierarchySearchAsync(string name);
    }
}