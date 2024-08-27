using System;
using Device.Consumer.KraftShared.Enums;

namespace Device.Consumer.KraftShared.Model
{
    public class IntegrationChangedMessage
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public ActionTypeEnum ActionType { get; set; }
    }
}
