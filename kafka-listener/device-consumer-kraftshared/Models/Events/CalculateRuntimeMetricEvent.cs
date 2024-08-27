using System.Collections.Generic;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;

namespace Device.Consumer.KraftShared.Events
{
    public class CalculateRuntimeMetricEvent : BusEvent
    {
        public override string TopicName => "device.application.event.calculate.runtime.metric.changed";

        public IDictionary<string, object> RawData { get; set; }

        public CalculateRuntimeMetricEvent(IDictionary<string, object> payload)
        {
            RawData = payload;
        }
    }
}
