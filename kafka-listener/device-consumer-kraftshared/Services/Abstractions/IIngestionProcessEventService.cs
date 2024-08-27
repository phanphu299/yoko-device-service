using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Confluent.Kafka;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Models.MetricModel;
using Device.Consumer.KraftShared.Service.Model;
using System.Linq;

namespace Device.Consumer.KraftShared.Service.Abstraction
{
    public interface IIngestionProcessEventService
    {
        Task<List<MetricValuesObject>> CalculateRuntimeMetricAsync(IEnumerable<IngestionMessage> ingesMessages, string projectId);
        Task<IEnumerable<DeviceInformation>> GetListDeviceInformationAsync(IEnumerable<IngestionMessage> messages, string projectId);
        Task ProcessEventAsync(IEnumerable<IngestionMessage> messages, IEnumerable<DeviceInformation> deviceInfos);
        Task<IEnumerable<DeviceInformation>> GetListDeviceInformationAsync(IngestionMessage message, string projectId);
    }
}
