using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class DeviceSignalQuality : IEntity<int>
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}