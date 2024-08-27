using System;
using System.Collections.Generic;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;

namespace AHI.Device.Function.Model
{
    public class TriggerFunctionBlockMessage : BusEvent
    {
        public Guid AssetId { get; set; }
        public Guid AttributeId { get; set; }
        public string TenantId { get; set; }
        public string ProjectId { get; set; }
        public string SubscriptionId { get; set; }
        public IEnumerable<Guid> FunctionBlockIds { get; set; }
        public long UnixTimestamp { get; set; }
        public override string TopicName => "device.application.command.trigger.function.block";
    }
}