using System.Collections.Generic;
using AHI.Device.Function.Model;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface IDataProcessor
    {
        IEnumerable<MetricSnapshot> Process(string timestamp, IEnumerable<DeviceMetric> deviceMetrics, IDictionary<string, object> payload, bool enableCompression = false);
        //object Process(string dataType, )
    }
}
