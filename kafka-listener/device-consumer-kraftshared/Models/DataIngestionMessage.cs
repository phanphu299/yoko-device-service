namespace Device.Consumer.KraftShared.Model
{
    public class DataIngestionMessage
    {
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public string FilePath { get; set; }
    }
}
