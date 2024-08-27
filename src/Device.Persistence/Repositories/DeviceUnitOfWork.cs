using Device.Application.Repository;
using Device.Persistence.DbContext;
using AHI.Infrastructure.Repository;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using Device.Domain.Entity;
namespace Device.Persistence.Repository
{
    public class DeviceUnitOfWork : BaseUnitOfWork, IDeviceUnitOfWork
    {
        public IDeviceRepository Devices { get; private set; }
        public IDeviceTemplateRepository DeviceTemplates { get; private set; }
        public ITemplateDetailRepository TemplateDetailRepository { get; }
        public IAssetAttributeTemplateRepository AssetAttributeTemplates { get; }
        public IAssetAttributeRepository AssetAttributes { get; }
        public IAssetAttributeSnapshotRepository AssetAttributeSnapshots { get; private set; }
        public  IEntityTagRepository<EntityTagDb> EntityTags { get; private set; }

        public DeviceUnitOfWork(
            DeviceDbContext context,
            IDeviceRepository deviceRepository,
            IDeviceTemplateRepository templateRepository,
            IAssetAttributeTemplateRepository assetAttributeTemplateRepository,
            IAssetAttributeRepository assetAttribute,
            ITemplateDetailRepository templateDetailRepository,
            IAssetAttributeSnapshotRepository assetAttributeSnapshot,
            IEntityTagRepository<EntityTagDb> entityTagRepository) : base(context)
        {
            Devices = deviceRepository;
            DeviceTemplates = templateRepository;
            TemplateDetailRepository = templateDetailRepository;
            AssetAttributeTemplates = assetAttributeTemplateRepository;
            AssetAttributes = assetAttribute;
            AssetAttributeSnapshots = assetAttributeSnapshot;
            EntityTags = entityTagRepository;
        }
    }
}
