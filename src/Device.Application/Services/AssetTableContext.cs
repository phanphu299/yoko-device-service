using System;
using System.Collections.Generic;
using AHI.Infrastructure.Service.Dapper.Model;
using Device.Application.Service.Abstraction;

namespace Device.Application.Service
{
    public class AssetTableContext : IAssetTableContext, IBlockAssetTableContext
    {
        private Guid _tableId;
        public Guid TableId => _tableId;

        private string _columnName;
        public string ColumnName => _columnName;

        private QueryCriteria _queryCriteria;
        public QueryCriteria QueryCriteria => _queryCriteria;

        private AggregationCriteria _aggregationCriteria;
        public AggregationCriteria AggregationCriteria => _aggregationCriteria;

        private IEnumerable<IDictionary<string, object>> _data;
        public IEnumerable<IDictionary<string, object>> Data => _data;

        private IEnumerable<object> _ids;
        public IEnumerable<object> Ids => _ids;

        public IBlockAssetTableContext SetTableId(Guid tableId)
        {
            _tableId = tableId;
            return this;
        }

        public IBlockAssetTableContext SetTableColumnName(string columnName)
        {
            _columnName = columnName;
            return this;
        }

        public IBlockAssetTableContext SetTableQuery(QueryCriteria queryCriteria)
        {
            _queryCriteria = queryCriteria;
            return this;
        }

        public IBlockAssetTableContext SetTableAggregation(AggregationCriteria aggregationCriteria)
        {
            _aggregationCriteria = aggregationCriteria;
            return this;
        }

        public IBlockAssetTableContext SetTableData(IEnumerable<IDictionary<string, object>> data)
        {
            _data = data;
            return this;
        }

        public IBlockAssetTableContext SetTableIds(IEnumerable<object> ids)
        {
            _ids = ids;
            return this;
        }
    }
}