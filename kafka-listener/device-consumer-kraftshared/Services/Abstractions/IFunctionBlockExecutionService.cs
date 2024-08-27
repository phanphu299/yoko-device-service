using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Model;

namespace Device.Consumer.KraftShared.Service.Abstraction
{
    public interface IFunctionBlockExecutionService
    {
        Task<IEnumerable<TriggerAttributeFunctionBlockExecution>> FindFunctionBlockExecutionByAssetIds(string tenantId, string subscriptionId, string projectId, IEnumerable<Guid> assetIds);
    }
}