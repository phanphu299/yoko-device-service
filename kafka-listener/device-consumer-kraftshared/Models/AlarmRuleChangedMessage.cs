using Device.Consumer.KraftShared.Enums;

namespace Device.Consumer.KraftShared.Model
{
    public class AlarmRuleChangedMessage
    {
        public string Id { get; set; }
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public ActionTypeEnum ActionType { get; set; }
        public string DeviceId { get; set; }
        public int MetricId { get; set; }
        public int TotalRule { get; set; }
    }
}
