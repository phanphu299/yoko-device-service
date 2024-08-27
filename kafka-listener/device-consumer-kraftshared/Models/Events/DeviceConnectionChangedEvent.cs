using System;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Enum;
using AHI.Infrastructure.MultiTenancy.Abstraction;

namespace Device.Consumer.KraftShared.Events
{
    public class DeviceConnectionChangedEvent : BusEvent
    {
        public override string TopicName => "device.application.event.device.status.changed";
        public string DeviceId { get; }
        public string Status { get; }
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
        public DeviceConnectionChangedEvent(string id, string status, ITenantContext tenantContext, ActionTypeEnum actionType = ActionTypeEnum.Updated)
        {
            DeviceId = id;
            Status = status;
            TenantId = tenantContext.TenantId;
            SubscriptionId = tenantContext.SubscriptionId;
            ProjectId = tenantContext.ProjectId;
            ActionType = actionType;
        }
    }
}