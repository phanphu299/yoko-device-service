using AHI.Infrastructure.MultiTenancy.Abstraction;

namespace Device.Job.Model
{
    public class QueueMessage
    {
        public ITenantContext TenantContext { get; set; }
        public JobInfo JobInfo { get; set; }

        public QueueMessage(ITenantContext tenantContext, JobInfo jobInfo)
        {
            TenantContext = tenantContext;
            JobInfo = jobInfo;
        }
    }
}