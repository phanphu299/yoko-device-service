using System.Linq;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;

namespace Device.Persistence.Repository
{
    public class HealthCheckMethodPersistenceRepository : IHealthCheckMethodRepository
    {
        private readonly DeviceDbContext _context;
        public HealthCheckMethodPersistenceRepository(DeviceDbContext context)
        {
            _context = context;
        }

        public IQueryable<HealthCheckMethod> AsQueryable()
        {
            return _context.HealthCheckMethods;
        }
    }
}
