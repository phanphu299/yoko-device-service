using System.Linq;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;

namespace Device.Persistence.Repository
{
    class TemplateKeyTypesPersistenceRepository : ITemplateKeyTypesRepository, IReadTemplateKeyTypesRepository
    {
        private readonly DeviceDbContext _context;
        public TemplateKeyTypesPersistenceRepository(DeviceDbContext context)
        {
            _context = context;
        }

        public IQueryable<TemplateKeyType> AsQueryable()
        {
            return _context.TemplateKeyTypes;
        }
    }
}
