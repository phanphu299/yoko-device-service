namespace AHI.Device.Function.Model
{
    public class ProjectDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
        public bool Deleted { get; set; }
        public bool IsMigrated { get; set; }
        public string ProjectType { get; set; }
    }
}