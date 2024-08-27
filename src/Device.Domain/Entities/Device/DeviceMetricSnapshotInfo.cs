using System;
using AHI.Infrastructure.Repository.Model.Generic;

namespace Device.Domain.Entity
{
    public class DeviceMetricSnapshotInfo : IEntity<int>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public string DeviceId { get; set; }

    }
}
