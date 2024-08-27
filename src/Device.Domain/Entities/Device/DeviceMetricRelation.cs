using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class DeviceMetricRelation : IEntity<int>
    {
        public int Id { get; set; }
        public string MetricName { get; set; }
        public int MetricId { get; set; }
        public string DeviceId { get; set; }
        public string DataType { get; set; }
        public string Expression { get; set; }
        public bool Enabled { get; set; }
    }
}
