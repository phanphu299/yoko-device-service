using System.Collections.Generic;

namespace AHI.Device.Function.Model
{
    public class DeviceSignalMessage
    {
        public string Domain { get; set; }
        public string DeviceId { get; set; }
        public IEnumerable<MetricSnapshot> Snapshots { get; set; }

        public DeviceSignalMessage(string domain, string deviceId, IEnumerable<MetricSnapshot> snapshots)
        {
            Domain = domain;
            DeviceId = deviceId;
            Snapshots = snapshots;
        }
    }
}