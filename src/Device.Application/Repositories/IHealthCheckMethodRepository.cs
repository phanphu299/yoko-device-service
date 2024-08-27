using AHI.Infrastructure.Repository.Generic;

namespace Device.Application.Repository
{
    public interface IHealthCheckMethodRepository : ISearchRepository<Domain.Entity.HealthCheckMethod, int>
    {
    }
}
