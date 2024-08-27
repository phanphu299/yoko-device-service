using System;
using System.Collections.Generic;
using AHI.Infrastructure.MultiTenancy.Abstraction;

namespace Device.Application.BlockFunction.Trigger.Model
{
    public class SchedulerTriggerDto : BlockExecutionTriggerDto
    {
        public Guid Id { get; set; }
        public string Cron { get; set; }
        public DateTime Start { get; set; } = DateTime.UtcNow;
        public DateTime? Expire { get; set; }
        public DateTime? End => Expire;
        public string Endpoint { get; set; }
        public string Method { get; set; }
        public string TimeZoneName { get; set; } = "GMT Standard Time";
        public IDictionary<string, string> AdditionalParams { get; set; }
        //public Guid? JobId { get; set; }
        public SchedulerTriggerDto()
        {
            AdditionalParams = new Dictionary<string, string>();
        }
        public SchedulerTriggerDto AppendTenantContextData(ITenantContext tenantContext)
        {
            AdditionalParams["tenantId"] = tenantContext.TenantId.ToString();
            AdditionalParams["subscriptionId"] = tenantContext.SubscriptionId.ToString();
            AdditionalParams["projectId"] = tenantContext.ProjectId.ToString();
            return this;
        }
    }
}