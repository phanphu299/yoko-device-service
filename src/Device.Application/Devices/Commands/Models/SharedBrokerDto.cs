using System;

namespace Device.Application.Device.Command.Model
{
    public class SharedBrokerDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string ProjectId { get; set; }
    }
}
