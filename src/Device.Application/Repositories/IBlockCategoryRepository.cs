using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Infrastructure.Repository.Generic;
using Device.Domain.Entity;

namespace Device.Application.Repository
{
    public interface IBlockCategoryRepository : IRepository<FunctionBlockCategory, Guid>
    {
        Task RetrieveAsync(IEnumerable<Domain.Entity.FunctionBlockCategory> categories);
    }
}