using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Models.MetricModel;
using Device.Consumer.KraftShared.Service.Model;

namespace Device.Consumer.KraftShared.Repositories.Abstraction.ReadOnly
{
    public interface IReadOnlyDeviceRepository
    {
        Task<IEnumerable<DeviceMetricDataType>> GetMetricDataTypesAsync(string deviceIdFromFile);
        Task<IEnumerable<Guid>> GetAssetIdsAsync(string projectId, string deviceId);
        Task<DeviceInformation> GetDeviceInformationAsync(string projectId, string[] deviceIds);
        Task<IEnumerable<DeviceInformation>> GetDeviceInformationsWithTopicNameAsync(string projectId, string topicName, string brokerType);
        Task<IEnumerable<string>> GetDeviceMetricKeyAsync(string projectId);
        Task<IEnumerable<(string MetricKey, string DataType)>> GetActiveDeviceMetricsAsync(string deviceId, IList<string> deviceMetrics);

        #region Cache Migration only
        Task LoadAllNecessaryResourcesAsync(string projectId);
        Task<IEnumerable<DeviceInformation>> GetProjectDevicesAsync(string projectId, bool forceToReload = false);
        Task<IDictionary<string, IEnumerable<AssetAttribute>>> GetProjectAssetAttributesAsync(string projectId, IDictionary<string, IEnumerable<AssetRuntimeTrigger>> triggers);
        Task<IDictionary<string, IEnumerable<AssetRuntimeTrigger>>> GetProjectAssetRuntimeTriggersAsync(string projectId, IEnumerable<string> deviceIds, bool forceToReload = false);
        Task<IDictionary<string, IEnumerable<AttributeSnapshot>>> GetProjectAttributeSnapshotsAsync(string projectId, IDictionary<string, IEnumerable<AssetAttribute>> assetsAttributes);
        Task<IEnumerable<AttributeSnapshot>> GetProjectAttributeSnapshotsAsync(string projectId, IEnumerable<AssetAttribute> assetsAttributes);
        Task<IEnumerable<DeviceAttributeSnapshot>> GetProjectDeviceAttributeSnapshotsAsync(string projectId, IEnumerable<AssetAttribute> assetsAttributes);
        Task<IEnumerable<DeviceAttributeSnapshot>> GetProjectDeviceAttributeSnapshotsAsync(string projectId, IEnumerable<AssetAttribute> assetsAttributes, IEnumerable<(Guid, Guid)> aliasMapping);
        Task<(Guid target, Guid source)> GetProjectTargetAttributeIdsAsync(string projectId, Guid aliasAttributeId);
        Task<IEnumerable<(Guid, Guid)>> GetProjectAttributeAliasAsync(string projectId, IEnumerable<AssetAttribute> assetAttributes);
        Task<IEnumerable<(Guid, Guid)>> GetProjectAttributeAliasMappingAsync(string projectId, IEnumerable<AssetAttribute> assetAttributes, IEnumerable<AssetRuntimeTrigger> assetRuntimeTriggers);
        #endregion
    }
}
