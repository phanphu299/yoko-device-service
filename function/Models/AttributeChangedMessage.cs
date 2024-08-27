using System;
using Function.Enum;

namespace AHI.Device.Function.Model
{
    public class AttributeChangedMessage
    {
        public Guid? Id { get; set; }
        public Guid? AttributeTemplateId { get; set; }
        public Guid AssetId { get; set; }
        public string Value { get; set; }
        public string IntegrationId { get; set; }
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public ActionTypeEnum ActionType { get; set; }
        public int DeviceExternalId { get; set; }
    }
}
