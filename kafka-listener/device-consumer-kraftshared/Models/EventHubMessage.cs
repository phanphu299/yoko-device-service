using System;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;

namespace Device.Consumer.KraftShared.Model
{
    public class EventHubMessage : BusEvent
    {
        public DateTime ReceivedAt { get; set; }
        public string Payload { get; set; }

        public override string TopicName => "iot.event.received";
    }
}