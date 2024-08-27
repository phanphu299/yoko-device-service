using AHI.Infrastructure.MultiTenancy.Abstraction;

namespace Device.ApplicationExtension.Extension
{
    public static class TenantContextExtension
    {
        public static void CopyFrom(this ITenantContext tenantContext, ITenantContext sourceTenantContext)
        {
            tenantContext.SetTenantId(sourceTenantContext.TenantId);
            tenantContext.SetSubscriptionId(sourceTenantContext.SubscriptionId);
            tenantContext.SetProjectId(sourceTenantContext.ProjectId);
        }
    }
}
