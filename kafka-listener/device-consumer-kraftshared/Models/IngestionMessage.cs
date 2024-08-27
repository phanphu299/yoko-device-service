
using System.Collections.Generic;

namespace Device.Consumer.KraftShared.Model
{
    public class IngestionMessage
    {
        public string TenantId { get; set; }
        public string SubscriptionId { get; set; }
        public string ProjectId { get; set; }
        public string TopicName { get; set; }

        /// <summary>
        /// Device Metrics
        /// </summary>
        public Dictionary<string, object> RawData { get; set; }
    }
}
