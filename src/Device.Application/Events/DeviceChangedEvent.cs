using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Enum;

namespace Device.Application.Events
{
    public class DeviceChangedEvent : BusEvent
    {
        public override string TopicName => "device.application.event.device.changed";
        public string Id { get; }
        public int TotalDevice { get; }
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public bool IsRetentionDayChanged { get; set; }
        public DeviceChangedEvent(string id, int totalDevice, bool isRetentionDayChanged, ITenantContext tenantContext, ActionTypeEnum actionType = ActionTypeEnum.Created)
        {
            Id = id;
            TotalDevice = totalDevice;
            TenantId = tenantContext.TenantId;
            SubscriptionId = tenantContext.SubscriptionId;
            ProjectId = tenantContext.ProjectId;
            ActionType = actionType;
            IsRetentionDayChanged = isRetentionDayChanged;
        }
        // public static DeviceChangedEvent CreateFrom(Domain.Entity.Device entity, int totalDevice, bool isRetentionDayChanged, ITenantContext tenantContext, ActionTypeEnum actionType = ActionTypeEnum.Created)
        // {
        //     return new DeviceChangedEvent(entity.Id, totalDevice, isRetentionDayChanged, tenantContext, actionType);
        // }
    }
}
