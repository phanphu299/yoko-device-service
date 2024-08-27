using System;
using System.Collections.Generic;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;

namespace Device.Application.Events
{
    public class FileExportEvent : BusEvent
    {
        public override string TopicName => "device.application.event.file.exported";
        public Guid ActivityId { get; set; } = Guid.NewGuid();
        public string ObjectType { get; set; }
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public IEnumerable<string> Ids { get; set; }
        public string RequestedBy { get; set; }
        public string DateTimeFormat { get; set; }
        public string DateTimeOffset { get; set; }

        public FileExportEvent(Guid activityId, string objectType, IEnumerable<string> ids, ITenantContext tenantContext, string requestedBy,
                               string dateTimeFormat = null, string dateTimeOffset = null)
        {
            ActivityId = activityId;
            ObjectType = objectType;
            TenantId = tenantContext.TenantId;
            SubscriptionId = tenantContext.SubscriptionId;
            ProjectId = tenantContext.ProjectId;
            Ids = ids;
            RequestedBy = requestedBy;
            DateTimeFormat = dateTimeFormat;
            DateTimeOffset = dateTimeOffset;
        }
    }
}
