using System.Collections.Generic;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;

namespace AHI.Device.Function.Events
{
    public class TrackingHeartBeatEvent : BusEvent
    {
        public override string TopicName => "device.application.event.tracking.heart.beat.changed";

        public IDictionary<string, object> RawData { get; set; }

        public TrackingHeartBeatEvent(IDictionary<string, object> payload)
        {
            RawData = payload;
        }
    }
}
