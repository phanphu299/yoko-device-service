using System;
using System.Collections.Generic;

namespace AHI.Device.Function.Model
{
    public class ImportFileMessage
    {
        public string ObjectType { get; set; }
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public IEnumerable<string> FileNames { get; set; }
        public string RequestedBy { get; set; }
        public string DateTimeFormat { get; set; }
        public string DateTimeOffset { get; set; }
        public Guid CorrelationId { get; set; }
    }
}
