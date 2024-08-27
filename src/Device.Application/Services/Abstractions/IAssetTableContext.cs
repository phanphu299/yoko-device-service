using System;
using System.Collections.Generic;
using AHI.Infrastructure.Service.Dapper.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IAssetTableContext : IBlockAssetTableContext
    {
        Guid TableId { get; }
        string ColumnName { get; }
        QueryCriteria QueryCriteria { get; }
        AggregationCriteria AggregationCriteria { get; }
        IEnumerable<IDictionary<string, object>> Data { get; }
        IEnumerable<object> Ids { get; }
        IBlockAssetTableContext SetTableId(Guid tableId);
        IBlockAssetTableContext SetTableQuery(QueryCriteria queryCriteria);
        IBlockAssetTableContext SetTableAggregation(AggregationCriteria aggregationCriteria);
        IBlockAssetTableContext SetTableColumnName(string columnName);
        IBlockAssetTableContext SetTableData(IEnumerable<IDictionary<string, object>> data);
        IBlockAssetTableContext SetTableIds(IEnumerable<object> ids);
    }
}