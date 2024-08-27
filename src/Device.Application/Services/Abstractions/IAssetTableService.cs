using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Application.BlockFunction.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IAssetTableService
    {
        Task<Guid?> FetchAssetTableAsync(Guid assetId, string tableName);
        Task<Guid?> FetchAssetTableByIdAsync(Guid assetId, Guid tableId);
        Task<IEnumerable<TargetConnector>> SearchAssetTableAsync(IEnumerable<Guid> assetIds);
    }
}
