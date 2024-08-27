using System;

namespace Device.Consumer.KraftShared.Model
{
    public class AssetTemplateMessage
    {
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public Guid Id { get; set; }
    }
}