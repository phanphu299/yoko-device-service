using System.Collections.Generic;

namespace Device.Consumer.KraftShared.Model
{
    public class DeviceInformation
    {
        public string DeviceId { get; set; }
        public int RetentionDays { get; set; }
        public bool EnableHealthCheck { get; set; }
        public IEnumerable<DeviceMetricDataType> Metrics { get; set; }
        public DeviceInformation()
        {
            Metrics = new List<DeviceMetricDataType>();
        }
    }
}