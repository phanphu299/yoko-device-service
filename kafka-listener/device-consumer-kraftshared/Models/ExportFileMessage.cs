using System.Collections.Generic;

namespace Device.Consumer.KraftShared.Model
{
    public class ExportFileMessage
    {
        public string ObjectType { get; set; }
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public IEnumerable<string> Ids { get; set; }
        public string RequestedBy { get; set; }
        public string DateTimeFormat { get; set; }
        public string DateTimeOffset { get; set; }
    }
}
