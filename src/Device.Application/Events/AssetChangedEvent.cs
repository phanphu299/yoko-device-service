using System;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Enum;

namespace Device.Application.Events
{
    public class AssetChangedEvent : BusEvent
    {
        public override string TopicName => "device.application.event.asset.changed";
        public Guid Id { get; }
        public Guid? ParentId { get; }
        public int TotalAsset { get; }
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public string Name { get; set; }
        public string ResourcePath { get; set; }
        public bool DeleteTable { get; set; }
        public bool DeleteMedia { get; set; }

        public AssetChangedEvent(Guid id, Guid? parentId, string name, string resourcePath, int totalAsset, ITenantContext tenantContext, ActionTypeEnum actionType = ActionTypeEnum.Created)
        {
            Id = id;
            ParentId = parentId;
            TotalAsset = totalAsset;
            TenantId = tenantContext.TenantId;
            SubscriptionId = tenantContext.SubscriptionId;
            ProjectId = tenantContext.ProjectId;
            ActionType = actionType;
            Name = name;
            ResourcePath = resourcePath;
        }
        public AssetChangedEvent(Guid id, Guid? parentId, string name, string resourcePath, bool deleteTable, bool deleteMedia, int totalAsset, ITenantContext tenantContext, ActionTypeEnum actionType = ActionTypeEnum.Created)
        {
            Id = id;
            ParentId = parentId;
            TotalAsset = totalAsset;
            TenantId = tenantContext.TenantId;
            SubscriptionId = tenantContext.SubscriptionId;
            ProjectId = tenantContext.ProjectId;
            ActionType = actionType;
            Name = name;
            ResourcePath = resourcePath;
            DeleteTable = deleteTable;
            DeleteMedia = deleteMedia;
        }
        public static AssetChangedEvent CreateFrom(Domain.Entity.Asset entity, int totalAsset, ITenantContext tenantContext, ActionTypeEnum actionType = ActionTypeEnum.Created)
        {
            return new AssetChangedEvent(entity.Id, entity.ParentAssetId, entity.Name, entity.ResourcePath, totalAsset, tenantContext, actionType);
        }
        // public static AssetChangedEvent CreateFrom(Domain.Entity.Asset entity, int totalAsset, ITenantContext tenantContext, ActionTypeEnum actionType = ActionTypeEnum.Created)
        // {
        //     return new AssetChangedEvent(entity.Id, entity.ParentAssetId, entity.Name, entity.ResourcePath, totalAsset, tenantContext, actionType);
        // }
    }
}
