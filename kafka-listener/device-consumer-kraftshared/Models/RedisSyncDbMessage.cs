
using System.Collections.Generic;
using Device.Consumer.KraftShared.Enums;

namespace Device.Consumer.KraftShared.Model
{
    public class RedisSyncDbMessage
    {
        public string TenantId { get; set; }
        public string SubscriptionId { get; set; }
        public string ProjectId { get; set; }
        public IEnumerable<string> Data { get; set; }
        public SnapshotEnum SnapshotType { get; set; }
    }
}
