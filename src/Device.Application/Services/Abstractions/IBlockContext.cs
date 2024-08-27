using System;
using System.Collections.Generic;
using AHI.Infrastructure.Service.Dapper.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IBlockContext
    {
        IBlockEngine BlockEngine { get; }
        IBlockContext SetBlockEngine(IBlockEngine engine);
        //  BlockExecutionMode ExecutionMode { get; }
        //string AssetName { get; }
        //IBlockContext SetAssetName(string name);
        object Value { get; }
        DateTime? DateTime { get; }
        string DataType { get; }
        IBlockContext SetValue(int value);
        IBlockContext SetValue(bool value);
        IBlockContext SetValue(double value);
        IBlockContext SetValue(string value);
        IBlockContext SetValue(DateTime value);
        IBlockContext SetValue(DateTime dateTime, int value);
        IBlockContext SetValue(DateTime dateTime, bool value);
        IBlockContext SetValue(DateTime dateTime, double value);
        IBlockContext SetValue(DateTime dateTime, string value);
        IBlockContext SetValue(params (DateTime dateTime, string value)[] values);
        IBlockContext SetValue(params (DateTime dateTime, int value)[] values);
        IBlockContext SetValue(params (DateTime dateTime, double value)[] values);
        IBlockContext SetValue(params (DateTime dateTime, bool value)[] values);
        Guid AssetId { get; }
        IBlockContext SetAssetId(Guid assetId);
        IBlockAttributeContext Attribute { get; }
        IBlockAttributeContext SetAttribute(IBlockAttributeContext attributeContext);
        IBlockAssetTableContext Table { get; }
        IBlockAssetTableContext SetTable(IBlockAssetTableContext assetTableContext);
        IBlockAssetTableContext SetTableQuery(QueryCriteria queryCriteria);
        IBlockAssetTableContext SetTableAggregation(AggregationCriteria aggregationCriteria);
        IBlockAssetTableContext SetTableColumnName(string columnName);
        IBlockAssetTableContext SetTableData(IEnumerable<IDictionary<string, object>> data);
        IBlockAssetTableContext SetTableIds(IEnumerable<object> ids);
        IBlockOperation BlockOperation { get; }
        IBlockContext SetBlockOperation(IBlockOperation blockOperation);
        bool IsArrayValue { get; }
        void CopyFrom(IBlockContext source);
        // IBlockHttpContext HttpContext { get; }
        // IBlockContext SetHttpContext(IBlockHttpContext context);
        //IBlockContext SetExecutionMode(BlockExecutionMode mode);
    }
}