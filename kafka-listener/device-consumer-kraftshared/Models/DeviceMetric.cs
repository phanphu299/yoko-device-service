namespace Device.Consumer.KraftShared.Model
{
    public class DeviceMetric
    {
        // public int Id { get; set; }
        public string DeviceId { get; set; }
        //public int MetricId { get; set; }
        public string MetricKey { get; set; }
        public string DataType { get; set; }
        public bool Enabled { get; set; } = true;

        //compression config
        public bool EnableDeadBand { get; set; }
        public bool EnableSwingDoor { get; set; }
        public int IdleTimeout { get; set; }
        public double ExDevPlus { get; set; }
        public double ExDevMinus { get; set; }
        public double CompDevPlus { get; set; }
        public double CompDevMinus { get; set; }
        // public string Expression { get; set; }
        public int RetentionDays { get; set; } = 90;
        //public bool IsRawMetric => string.IsNullOrEmpty(Expression);
        //public int AttributeId { get; set; }

    }
}