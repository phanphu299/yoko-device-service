using System;
using AHI.Infrastructure.Repository.Generic;

namespace Device.Application.Repository
{
    public interface IValidTemplateRepository : IRepository<Domain.Entity.ValidTemplate, Guid>
    {
    }
}
