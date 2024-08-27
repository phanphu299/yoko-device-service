using System;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;

namespace Device.Application.Events
{
    public class AssetTemplateChangedEvent : BusEvent
    {
        public override string TopicName => "device.application.event.asset.template.changed";
        public Guid Id { get; }
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }

        public AssetTemplateChangedEvent(Guid id, ITenantContext tenantContext)
        {
            Id = id;
            TenantId = tenantContext.TenantId;
            SubscriptionId = tenantContext.SubscriptionId;
            ProjectId = tenantContext.ProjectId;
        }
    }
}