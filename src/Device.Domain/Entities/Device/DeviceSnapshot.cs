using System;

namespace Device.Domain.Entity
{
    public class DeviceSnapshot
    {
        public string DeviceId { get; set; }
        public Device Device { get; set; }
        public string Status { get; set; }
        public DateTime? Timestamp { get; set; }
        public DateTime? CommandDataTimestamp { get; set; }
    }
}
