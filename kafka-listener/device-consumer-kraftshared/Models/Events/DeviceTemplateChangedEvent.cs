using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Enum;

namespace Device.Consumer.KraftShared.Events
{
    public class DeviceTemplateChangedEvent : BusEvent
    {
        public override string TopicName => "device.application.event.template.changed";
        public int Id { get; }
        public string Name { get; set; }
        public int TotalMetric { get; set; }
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public DeviceTemplateChangedEvent(int id, string name, int totalMetric, ITenantContext tenantContext, ActionTypeEnum actionType = ActionTypeEnum.Created)
        {
            Id = id;
            Name = name;
            TotalMetric = totalMetric;
            TenantId = tenantContext.TenantId;
            SubscriptionId = tenantContext.SubscriptionId;
            ProjectId = tenantContext.ProjectId;
            ActionType = actionType;
        }
    }
}
