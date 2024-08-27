using Device.Application.Repository;
using Device.Persistence.DbContext;
using AHI.Infrastructure.Repository;
using Device.Domain.Entity;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;

namespace Device.Persistence.Repository
{
    public class UomUnitOfWork : BaseUnitOfWork, IUomUnitOfWork
    {
        private IUomRepository _uom;
        public IUomRepository Uoms => _uom;
        public IEntityTagRepository<EntityTagDb> EntityTags { get; }

        public UomUnitOfWork(
            DeviceDbContext context,
            IUomRepository uomRepository,
            IEntityTagRepository<EntityTagDb> entityTags) : base(context)
        {
            _uom = uomRepository;
            EntityTags = entityTags;
        }
    }
}
