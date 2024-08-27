using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Device.Function.Model;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface IRuntimeAttributeService
    {
        Task<IEnumerable<Guid>> CalculateRuntimeValueAsync(string projectId, DateTime timestamp, IEnumerable<AssetRuntimeTrigger> assetRuntimeTriggers);
    }
}