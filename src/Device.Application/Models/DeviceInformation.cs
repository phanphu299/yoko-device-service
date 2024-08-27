using System;
using System.Collections.Generic;
using System.Text;

namespace Device.Application.Models
{
    public class DeviceInformation
    {
        public string DeviceId { get; set; }
        public int RetentionDays { get; set; }
        public bool EnableHealthCheck { get; set; }
        public IEnumerable<DeviceMetricDataType> Metrics { get; set; }
        public DeviceInformation()
        {
            Metrics = new List<DeviceMetricDataType>();
        }
    }

    public class DeviceMetricDataType
    {
        public string MetricKey { get; set; }
        public string DataType { get; set; }
        public string Value { get; set; }
        public string MetricType { get; set; }
        public string ExpressionCompile { get; set; }

        public string SourceDeviceId { get; set; }
    }
}
