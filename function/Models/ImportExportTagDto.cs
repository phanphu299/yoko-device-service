using Newtonsoft.Json;

namespace AHI.Device.Function.Model
{
    public class ImportExportTagDto
    {
        [JsonIgnore]
        public long TagId { get; set; }

        public string Key { get; set; }

        public string Value { get; set; }
    }
}
