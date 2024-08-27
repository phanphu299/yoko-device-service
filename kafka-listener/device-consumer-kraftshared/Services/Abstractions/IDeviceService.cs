using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Model;

namespace Device.Consumer.KraftShared.Service.Abstraction
{
    public interface IDeviceService
    {
        Task<(long, DateTime?, IEnumerable<Guid>)> ProcessEventAsync(IngestionMessage ingestionMessage);
        // Task<IEnumerable<Guid>> GetAssetIdsAsync(string projectId, string deviceId);
        //Task<IEnumerable<AssetRuntimeTrigger>> GetAssetTriggerAsync(string projectId, string deviceId);
    }
}
