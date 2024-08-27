namespace AHI.Device.Function.Model
{
    public class ValueObject
    {
        public string Timestamp { get; set; }
        public object Value { get; set; }

        public ValueObject(string timestamp, object value)
        {
            Timestamp = timestamp;
            Value = value;
        }
    }
}