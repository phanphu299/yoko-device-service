using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Device.Function.Model;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface IFunctionBlockExecutionService
    {
        Task<IEnumerable<TriggerAttributeFunctionBlockExecution>> FindFunctionBlockExecutionByAssetIds(IEnumerable<Guid> assetIds);
    }
}