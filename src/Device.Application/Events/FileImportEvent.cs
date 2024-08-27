using System.Collections.Generic;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using System;

namespace Device.Application.Events
{
    public class FileImportEvent : BusEvent
    {
        public override string TopicName => "device.application.event.file.imported";
        public string ObjectType { get; set; }
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public IEnumerable<string> FileNames { get; set; }
        public string RequestedBy { get; set; }
        public string DateTimeFormat { get; set; }
        public string DateTimeOffset { get; set; }
        public Guid CorrelationId { get; set; }

        public FileImportEvent(string objectType, IEnumerable<string> fileNames, ITenantContext tenantContext, string requestedBy, string dateTimeFormat, string datetimeOffset)
        {
            ObjectType = objectType;
            TenantId = tenantContext.TenantId;
            SubscriptionId = tenantContext.SubscriptionId;
            ProjectId = tenantContext.ProjectId;
            FileNames = fileNames;
            RequestedBy = requestedBy;
            DateTimeFormat = dateTimeFormat;
            DateTimeOffset = datetimeOffset;
        }
    }
}
