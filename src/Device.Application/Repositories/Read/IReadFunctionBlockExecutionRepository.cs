using System;
using AHI.Infrastructure.Repository.Generic;
using Device.Domain.Entity;
namespace Device.Application.Repository
{
    public interface IReadFunctionBlockExecutionRepository : IRepository<FunctionBlockExecution, Guid>
    {
      
    }
}