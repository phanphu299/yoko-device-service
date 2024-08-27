using Newtonsoft.Json;

namespace Device.Application.Device.Command.Model
{
    public class BrokerContentDto
    {
        [JsonProperty("enable_sharing")]
        public bool EnableSharing { get; set; }

        [JsonProperty("password_length")]
        public int PasswordLength { get; set; }
    }
}
