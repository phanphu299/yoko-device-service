using System;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Enum;

namespace Function.Event
{
    public class AssetAttributeChangedEvent : BusEvent
    {
        public override string TopicName => "device.application.event.asset.attribute.changed";
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public Guid AssetId { get; set; }
        public long UnixTimestamp { get; set; }
        public bool ForceReload { get; set; }
        
        public AssetAttributeChangedEvent(Guid assetId, long unixTimestamp, ITenantContext tenantContext, ActionTypeEnum actionType = ActionTypeEnum.Updated, bool forceReload = false)
        {
            AssetId = assetId;
            UnixTimestamp = unixTimestamp;
            TenantId = tenantContext.TenantId;
            SubscriptionId = tenantContext.SubscriptionId;
            ProjectId = tenantContext.ProjectId;
            ActionType = actionType;
            ForceReload = forceReload;
        }
    }
}