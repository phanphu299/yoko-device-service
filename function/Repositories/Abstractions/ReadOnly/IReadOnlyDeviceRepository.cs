using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Device.Function.Model;

namespace AHI.Infrastructure.Repository.Abstraction.ReadOnly
{
    public interface IReadOnlyDeviceRepository
    {
        Task<IEnumerable<string>> GetDeviceMetricKeyAsync(string projectId);
        Task<DeviceInformation> GetDeviceInformationAsync(string projectId, string[] deviceIds);
        Task<IEnumerable<DeviceInformation>> GetDeviceInformationsWithTopicNameAsync(string projectId, string topicName, string brokerType);
        Task<IEnumerable<MetricSeriesDto>> GetMetricNumericsAsync(string deviceIdFromFile);
        Task<IEnumerable<MetricSeriesTextDto>> GetMetricTextsAsync(string deviceIdFromFile);
        Task<IEnumerable<(string MetricKey, string DataType)>> GetActiveDeviceMetricsAsync(string projectId, string deviceId, IList<string> deviceMetrics);
        Task<IEnumerable<(string MetricKey, string DataType, DateTime? Timestamp)>> GetActiveDeviceMetricsWithTimestampsAsync(string projectId, string deviceId, IList<string> deviceMetrics);
    }
}