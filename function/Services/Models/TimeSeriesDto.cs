using Newtonsoft.Json;

namespace AHI.Device.Function.Service.Model
{
    public class TimeSeriesDto
    {
        [JsonProperty(PropertyName = "ts")]
        public long Timestamp { get; set; }

        [JsonProperty(PropertyName = "v")]
        public object Value { get; set; }
    }
}
