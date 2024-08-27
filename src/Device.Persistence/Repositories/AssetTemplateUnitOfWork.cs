using AHI.Infrastructure.Repository;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using Device.Application.Repository;
using Device.Domain.Entity;
using Device.Persistence.DbContext;

namespace Device.Persistence.Repository
{
    public class AssetTemplateUnitOfWork : BaseUnitOfWork, IAssetTemplateUnitOfWork
    {
        public IAssetAttributeTemplateRepository Attributes { get; private set; }
        public IDeviceTemplateRepository DeviceTemplates { get; private set; }

        public IAssetTemplateRepository Templates { get; private set; }

        //public IIntegrationRepository Integrations { get; private set; }

        public IAssetRepository Assets { get; private set; }
        public IAssetAttributeRepository AssetAttributes { get; private set; }

        public IAssetAttributeAliasRepository Alias { get; private set; }
        public ITemplateDetailRepository TemplateDetails { get; private set; }
        public  IEntityTagRepository<EntityTagDb> EntityTags { get; private set; }

        public AssetTemplateUnitOfWork(DeviceDbContext context,
            IAssetAttributeTemplateRepository attributes,
            IDeviceTemplateRepository deviceTemplates,
            IAssetTemplateRepository templates,
            IAssetRepository assets,
            IAssetAttributeRepository assetAttributes,
            IAssetAttributeAliasRepository alias,
            ITemplateDetailRepository templateDetails,
            IEntityTagRepository<EntityTagDb> entityTagRepository) : base(context)
        {
            Attributes = attributes;
            DeviceTemplates = deviceTemplates;
            Templates = templates;
            Assets = assets;
            AssetAttributes = assetAttributes;
            Alias = alias;
            TemplateDetails = templateDetails;
            EntityTags = entityTagRepository;
        }
    }
}
