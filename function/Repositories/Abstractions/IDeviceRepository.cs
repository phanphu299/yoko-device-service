using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Device.Function.Model;

namespace AHI.Infrastructure.Repository.Abstraction
{
    public interface IDeviceRepository
    {
        Task<IEnumerable<AssetRuntimeTrigger>> GetAssetTriggerAsync(string projectId, string deviceId);
        Task<IEnumerable<Guid>> GetAssetIdsAsync(string projectId, string deviceId);
        Task<DeviceInformation> GetDeviceInformationAsync(string projectId, string[] deviceIds);
        Task<IEnumerable<DeviceInformation>> GetDeviceInformationsWithTopicNameAsync(string projectId, string topicName, string brokerType);
        Task<IEnumerable<string>> GetDeviceMetricKeyAsync(string projectId);
        Task<IEnumerable<DeviceMetricDataType>> GetMetricDataTypesAsync(string deviceIdFromFile);
        Task<IEnumerable<(string MetricKey, string DataType)>> GetActiveDeviceMetricsAsync(string projectId, string deviceId, IList<string> deviceMetrics);
    }
}