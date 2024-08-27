using System;

namespace Device.Application.Device.Command.Model
{
    public class BrokerTopicDto
    {
        public Guid ClientId { get; set; }
        public string Topic { get; set; }
    }
}
