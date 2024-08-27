using System;
using AHI.Infrastructure.Repository.Generic;
namespace Device.Application.Repository
{
    public interface IReadBlockSnippetRepository : IRepository<Domain.Entity.FunctionBlockSnippet, Guid>
    {
    }
}