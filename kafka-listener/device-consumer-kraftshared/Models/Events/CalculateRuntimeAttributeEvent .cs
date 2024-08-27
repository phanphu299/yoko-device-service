using System.Collections.Generic;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;

namespace Device.Consumer.KraftShared.Events
{
    public class CalculateRuntimeAttributeEvent : BusEvent
    {
        public override string TopicName => "device.application.event.calculate.runtime.attribute.changed";

        public IDictionary<string, object> RawData { get; set; }

        public CalculateRuntimeAttributeEvent(IDictionary<string, object> payload)
        {
            RawData = payload;
        }
    }
}
