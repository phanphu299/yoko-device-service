using System;

namespace Device.Consumer.KraftShared.Model
{
    public class BrokerDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ProjectId { get; set; }
        public string Type { get; set; }
    }
}