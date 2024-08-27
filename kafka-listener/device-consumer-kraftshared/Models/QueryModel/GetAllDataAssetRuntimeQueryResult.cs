using System.Collections.Generic;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Service;
using Device.Consumer.KraftShared.Service.Model;
using Device.Consumer.KraftShared.Models.MetricModel;

namespace Device.Consumer.KraftShared.Models.QueryModel
{
    public class GetAllDataAssetRuntimeQueryResult
    {
        public IngestionMessage MetricDict { get; set; }
        public IEnumerable<DeviceInformation> DeviceInformations { get; set; }
        public Dictionary<string, IEnumerable<AssetAttribute>> AssetAttributes { get; set; }
        public Dictionary<string, IEnumerable<AssetRuntimeTrigger>> AssetRuntimeTriggers { get; set; }
        public Dictionary<string, IEnumerable<RuntimeValueObject>> AssetRuntimeValues { get; set; }
        public Dictionary<string, long> DataUnixTimestamps { get; set; }
    }
}
