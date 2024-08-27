using AHI.Infrastructure.Repository.Generic;
using AHI.Infrastructure.Service.Tag.Service.Abstraction;
using Device.Domain.Entity;

namespace Device.Application.Repository
{
    public interface IAssetUnitOfWork : IUnitOfWork
    {
        IAssetTemplateRepository Templates { get; }
        IAssetRepository Assets { get; }
        IAssetAttributeRepository AssetAttributes { get; }
        IAssetAttributeAliasRepository Alias { get; }
        IAssetAttributeTemplateRepository AssetAttributeTemplates { get; }
        IDeviceRepository Devices { get; }
        IDeviceTemplateRepository DeviceTemplates { get; }
        ITemplateDetailRepository TemplateDetails { get; }
        IEntityTagRepository<EntityTagDb> EntityTags { get; }
        AHI.Infrastructure.Service.Tag.Service.Abstraction.IEntityTagRepository<EntityTagDb> SharedEntityTags { get; }
    }
}