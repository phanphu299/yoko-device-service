using System;
using AHI.Infrastructure.Repository.Generic;

namespace Device.Application.Repository
{
    public interface IBlockSnippetRepository : IRepository<Domain.Entity.FunctionBlockSnippet, Guid>
    {
    }
}
