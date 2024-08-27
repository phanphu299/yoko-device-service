using Newtonsoft.Json;

namespace Device.Consumer.KraftShared.Service.Model
{
    public class TimeSeriesDto
    {
        [JsonProperty(PropertyName = "ts")]
        public long Timestamp { get; set; }

        [JsonProperty(PropertyName = "v")]
        public object Value { get; set; }
    }
}
