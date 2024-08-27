namespace Device.Consumer.KraftShared.Model
{

    public class DeviceChangeStatusMessage
    {
        public string DeviceId { get; set; }
        public object Value { get; set; }
        public int AttributeId { get; set; }
        public int DeviceMetricId { get; set; }
    }
}