using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class HealthCheckMethod : IEntity<int>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Deleted { get; set; }
    }
}
