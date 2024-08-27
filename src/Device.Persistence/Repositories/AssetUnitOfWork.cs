using AHI.Infrastructure.Repository;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;

namespace Device.Persistence.Repository
{
    public class AssetUnitOfWork : BaseUnitOfWork, IAssetUnitOfWork
    {
        public IAssetAttributeRepository AssetAttributes { get; private set; }
        public IAssetAttributeAliasRepository Alias { get; private set; }
        public IAssetRepository Assets { get; private set; }
        public IAssetTemplateRepository Templates { get; private set; }
        public IDeviceRepository Devices { get; private set; }
        public IDeviceTemplateRepository DeviceTemplates { get; private set; }
        public IAssetAttributeTemplateRepository AssetAttributeTemplates { get; private set; }
        public ITemplateDetailRepository TemplateDetails { get; private set; }
        public  IEntityTagRepository<EntityTagDb> EntityTags { get; private set; }
        public AHI.Infrastructure.Service.Tag.Service.Abstraction.IEntityTagRepository<EntityTagDb> SharedEntityTags { get; private set; }

        public AssetUnitOfWork(DeviceDbContext context,
            IAssetAttributeRepository elementAttributes,
            IAssetAttributeAliasRepository assetAliasAttributeRepository,
            IAssetRepository assetRepository,
            IAssetTemplateRepository assetTemplateRepository,
            IDeviceRepository deviceRepository,
            IDeviceTemplateRepository templateRepository,
            IAssetAttributeTemplateRepository assetAttributeTemplates,
            ITemplateDetailRepository templateDetailRepository,
            IEntityTagRepository<EntityTagDb> entityTagRepository,
            AHI.Infrastructure.Service.Tag.Service.Abstraction.IEntityTagRepository<EntityTagDb> sharedEntityTags
            ) : base(context)

        {
            AssetAttributes = elementAttributes;
            Alias = assetAliasAttributeRepository;
            Assets = assetRepository;
            Templates = assetTemplateRepository;
            Devices = deviceRepository;
            DeviceTemplates = templateRepository;
            AssetAttributeTemplates = assetAttributeTemplates;
            TemplateDetails = templateDetailRepository;
            EntityTags = entityTagRepository;
            SharedEntityTags = sharedEntityTags;
        }
    }
}
