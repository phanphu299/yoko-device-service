using System.Collections.Generic;

namespace Device.Consumer.KraftShared.Model
{
    public class AttributeAttributeTriggerRequest
    {
        public string TenantId { get; set; }
        public string SubscriptionId { get; set; }
        public string ProjectId { get; set; }
        public IEnumerable<AttributeCalculationRequest> Assets { get; set; }
    }
}
