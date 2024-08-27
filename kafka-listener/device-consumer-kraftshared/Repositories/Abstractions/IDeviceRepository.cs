using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Service.Model;

namespace Device.Consumer.KraftShared.Repositories.Abstraction
{
    public interface IDeviceRepository
    {
        Task<IEnumerable<AssetRuntimeTrigger>> GetAssetTriggerAsync(string projectId, string deviceId, bool forceToReload = false);
        Task<IEnumerable<AssetAttribute>> GetAssetAttributesAsync(string projectId, string deviceId, IEnumerable<AssetRuntimeTrigger> triggers);
        Task<IEnumerable<Guid>> GetAssetIdsAsync(string projectId, string deviceId);
    }
}
