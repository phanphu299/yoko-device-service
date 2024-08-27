using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface IDeviceService
    {
        Task<(long, DateTime?, IEnumerable<Guid>)> ProcessEventAsync(IDictionary<string, object> metricDict);
        // Task<IEnumerable<Guid>> GetAssetIdsAsync(string projectId, string deviceId);
        //Task<IEnumerable<AssetRuntimeTrigger>> GetAssetTriggerAsync(string projectId, string deviceId);
    }
}
