using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using Device.Domain.Entity;

namespace Device.Application.Repository
{
    public interface IUomUnitOfWork : AHI.Infrastructure.Repository.Generic.IUnitOfWork
    {
        IUomRepository Uoms { get; }
        IEntityTagRepository<EntityTagDb> EntityTags { get; }
    }
}
