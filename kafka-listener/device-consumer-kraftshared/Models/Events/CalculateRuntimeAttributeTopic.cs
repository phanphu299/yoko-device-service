using System.Collections.Generic;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;

namespace Device.Consumer.KraftShared.Events
{
    public class CalculateRuntimeAttributeTopic: BusEvent
    {
        public IDictionary<string, object> RawData { get; set; }

        public override string TopicName => "calculate_runtime_attribute";

        public CalculateRuntimeAttributeTopic(IDictionary<string, object> payload)
        {
            RawData = payload;
        }
    }
}
