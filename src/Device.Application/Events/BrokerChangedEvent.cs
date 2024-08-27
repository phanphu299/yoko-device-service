using System;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Enum;

namespace Device.Application.Events
{
    public class BrokerChangedEvent : BusEvent
    {
        public override string TopicName => "broker.application.event.broker.changed";
        public Guid Id { get; }
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public string Type { get; set; }
        public bool RequestDeploy { get; set; }
        public BrokerChangedEvent(Guid id, ITenantContext tenantContext, bool requestDeploy, ActionTypeEnum actionType = ActionTypeEnum.Updated)
        {
            Id = id;
            TenantId = tenantContext.TenantId;
            SubscriptionId = tenantContext.SubscriptionId;
            ProjectId = tenantContext.ProjectId;
            ActionType = actionType;
            Type = actionType.ToString();
            RequestDeploy = requestDeploy;
        }
    }
}
