using System;
using Function.Enum;

namespace AHI.Device.Function.Model
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
