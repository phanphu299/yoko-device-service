using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using Device.Domain.Entity;

namespace Device.Application.Repository
{
    public interface IDeviceUnitOfWork : AHI.Infrastructure.Repository.Generic.IUnitOfWork
    {
        IDeviceRepository Devices { get; }
        IDeviceTemplateRepository DeviceTemplates { get; }
        ITemplateDetailRepository TemplateDetailRepository { get; }
        IAssetAttributeTemplateRepository AssetAttributeTemplates { get; }
        IAssetAttributeRepository AssetAttributes { get; }
        IAssetAttributeSnapshotRepository AssetAttributeSnapshots { get; }
        IEntityTagRepository<EntityTagDb> EntityTags { get; }
    }
}
