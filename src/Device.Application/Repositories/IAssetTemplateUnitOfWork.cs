using AHI.Infrastructure.Repository.Generic;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using Device.Domain.Entity;

namespace Device.Application.Repository
{
    public interface IAssetTemplateUnitOfWork : IUnitOfWork
    {
        IAssetAttributeTemplateRepository Attributes { get; }
        IAssetTemplateRepository Templates { get; }
        IAssetRepository Assets { get; }
        IAssetAttributeRepository AssetAttributes { get; }
        IAssetAttributeAliasRepository Alias { get; }
        IDeviceTemplateRepository DeviceTemplates { get; }
        ITemplateDetailRepository TemplateDetails { get; }
        IEntityTagRepository<EntityTagDb> EntityTags { get; }
    }
}
