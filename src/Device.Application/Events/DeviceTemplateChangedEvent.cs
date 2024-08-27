using System;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Enum;

namespace Device.Application.Events
{
    public class DeviceTemplateChangedEvent : BusEvent
    {
        public override string TopicName => "device.application.event.template.changed";
        public Guid Id { get; }
        public string Name { get; set; }
        public int TotalMetric { get; set; }
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public DeviceTemplateChangedEvent(Guid id, string name, int totalMetric, ITenantContext tenantContext, ActionTypeEnum actionType = ActionTypeEnum.Created)
        {
            Id = id;
            Name = name;
            TotalMetric = totalMetric;
            TenantId = tenantContext.TenantId;
            SubscriptionId = tenantContext.SubscriptionId;
            ProjectId = tenantContext.ProjectId;
            ActionType = actionType;
        }
        public static DeviceTemplateChangedEvent CreateFrom(Domain.Entity.DeviceTemplate entity, int totalMetric, ITenantContext tenantContext, ActionTypeEnum actionType = ActionTypeEnum.Created)
        {
            return new DeviceTemplateChangedEvent(entity.Id, entity.Name, totalMetric, tenantContext, actionType);
        }
    }
}
