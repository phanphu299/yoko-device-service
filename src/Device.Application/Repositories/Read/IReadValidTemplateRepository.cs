using System;
using AHI.Infrastructure.Repository.Generic;
namespace Device.Application.Repository
{
    public interface IReadValidTemplateRepository : IRepository<Domain.Entity.ValidTemplate, Guid>
    {
    }
}