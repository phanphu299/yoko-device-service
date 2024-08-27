using System;
using System.Threading.Tasks;
using Device.Application.BlockFunction.Model;

namespace Device.Application.Repository
{
    public interface IBlockExecutionRepository
    {
        Task<BlockQueryResult> GetAssetAttributeSnapshotAsync(Guid assetId, Guid attributeId);
        Task<BlockQueryResult> GetNearestAssetAttributeValueAsync(Guid assetId, Guid attributeId, DateTime dateTime, string padding);
        Task<double> GetLastTimeDiffAssetAttributeValueAsync(Guid assetId, Guid attributeId, string filterUnit);
        Task<double> GetLastValueDiffAssetAttributeValueAsync(Guid assetId, Guid attributeId, string filterUnit);
        Task<double> GetTimeDiff2PointsAssetAttributeValueAsync(Guid assetId, Guid attributeId, DateTime start, DateTime end, string filterUnit);
        Task<double> GetValueDiff2PointsAssetAttributeValueAsync(Guid assetId, Guid attributeId, DateTime start, DateTime end);
        Task SetAssetAttributeValueAsync(Guid assetId, Guid attributeId, params BlockDataRequest[] values);
        //Task CanSetAssetAttributeValueAsync(Guid assetId, Guid attributeId, params BlockDataRequest[] values);
        Task<double> AggregateAssetAttributesValueAsync(Guid assetId, Guid attributeId, DateTime start, DateTime end, string aggregate, string filterOperation, object filterValue);
        Task<double> GetDurationAssetAttributeValueAsync(Guid assetId, Guid attributeId, DateTime start, DateTime end, string filterOperation, object filterValue, string filterUnit);
        Task<int> GetCountAssetAttributeValueAsync(Guid assetId, Guid attributeId, DateTime start, DateTime end, string filterOperation, object filterValue);
        //Task<IEnumerable<IDictionary<string, object>>> QueryAssetTableValueAsync(Guid assetId, Guid tableId, QueryCriteria query);
        // Task<BaseResponse> UpsertAssetTableValueAsync(Guid assetId, Guid tableId, IEnumerable<IDictionary<string, object>> data);
        // Task<BaseResponse> DeleteAssetTableValueAsync(Guid assetId, Guid tableId, IEnumerable<object> ids);
        // Task<object> AggregateAssetTableValueAsync(Guid assetId, Guid tableId, string columnName, AggregationCriteria aggregationCriteria);
        //Task<IEnumerable<string>> GetAllChildrenAsync(string assetName);
    }
}