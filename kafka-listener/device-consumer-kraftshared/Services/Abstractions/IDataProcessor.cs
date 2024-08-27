using System.Collections.Generic;
using Device.Consumer.KraftShared.Model;

namespace Device.Consumer.KraftShared.Service.Abstraction
{
    public interface IDataProcessor
    {
        IEnumerable<MetricSnapshot> Process(string timestamp, IEnumerable<DeviceMetric> deviceMetrics, IDictionary<string, object> payload, bool enableCompression = false);
        //object Process(string dataType, )
    }
}
