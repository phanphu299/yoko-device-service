namespace AHI.Device.Function.Model
{
    public class DeviceMetricDataType
    {
        public string MetricKey { get; set; }
        public string DataType { get; set; }
        public string Value { get; set; }
        public string MetricType { get; set; }
        public string ExpressionCompile { get; set; }
        //public Guid DetailId { get; set; }
    }
}