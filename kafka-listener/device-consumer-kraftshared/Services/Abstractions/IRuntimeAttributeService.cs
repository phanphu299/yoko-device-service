using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Model;

namespace Device.Consumer.KraftShared.Service.Abstraction
{
    public interface IRuntimeAttributeService
    {
        Task<IEnumerable<Guid>> CalculateRuntimeValueAsync(string projectId, DateTime timestamp, IEnumerable<AssetRuntimeTrigger> assetRuntimeTriggers);
    }
}