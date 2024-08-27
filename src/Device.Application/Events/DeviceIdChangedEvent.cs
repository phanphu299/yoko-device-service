using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Enum;
using System;

namespace Device.Application.Events
{
    public class DeviceIdChangedEvent : BusEvent
    {
        public override string TopicName => "device.application.event.device.id.changed";
        public string OldId { get; }
        public string NewId { get; }
        public string Name { get; }
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

        public DeviceIdChangedEvent(string oldId, string newId, string name, ITenantContext tenantContext, ActionTypeEnum actionType = ActionTypeEnum.Created)
        {
            OldId = oldId;
            NewId = newId;
            Name = name;
            TenantId = tenantContext.TenantId;
            SubscriptionId = tenantContext.SubscriptionId;
            ProjectId = tenantContext.ProjectId;
            ActionType = actionType;
        }
    }
}
