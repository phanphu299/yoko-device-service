using System;
using System.Linq;
using System.Collections.Generic;
using AHI.Infrastructure.Service.Dapper.Model;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using Device.Application.Asset.Command.Model;
using Device.ApplicationExtension.Extension;

namespace Device.Application.Service
{
    public class BlockContext : IBlockContext
    {
        public BlockContext(IBlockEngine engine)
        {
            _engine = engine;
        }
        private Guid _assetId;
        public Guid AssetId => _assetId;
        private object _value;
        public object Value => _value;
        private IBlockEngine _engine;
        public IBlockEngine BlockEngine => _engine;
        private IBlockAttributeContext _attribute;
        public IBlockAttributeContext Attribute => _attribute;
        private IBlockAssetTableContext _table;
        public IBlockAssetTableContext Table => _table;
        private IBlockOperation _blockOperation;

        public IBlockOperation BlockOperation => _blockOperation;
        private DateTime? _dateTime;
        public DateTime? DateTime => _dateTime;
        private string _dataType;
        public string DataType => _dataType;
        private bool _isArrayValue = false;
        public bool IsArrayValue => _isArrayValue;

        public IBlockContext SetBlockOperation(IBlockOperation blockOperation)
        {
            _blockOperation = blockOperation;
            return this;
        }
        public IBlockContext SetBlockEngine(IBlockEngine engine)
        {
            _engine = engine;
            return this;
        }
        public IBlockContext SetAssetId(Guid assetId)
        {
            _assetId = assetId;
            return this;
        }
        public IBlockAttributeContext SetAttribute(IBlockAttributeContext attributeContext)
        {
            _attribute = attributeContext;
            return attributeContext;
        }

        public IBlockAssetTableContext SetTable(IBlockAssetTableContext assetTableContext)
        {
            _table = assetTableContext;
            return _table;
        }

        public IBlockAssetTableContext SetTableQuery(QueryCriteria queryCriteria)
        {
            IAssetTableContext tableContext = null;
            if (_table == null)
                tableContext = new AssetTableContext();
            else
                tableContext = _table as AssetTableContext;

            tableContext.SetTableQuery(queryCriteria);
            _table = tableContext;

            return _table;
        }

        public IBlockAssetTableContext SetTableAggregation(AggregationCriteria aggregationCriteria)
        {
            IAssetTableContext tableContext = null;
            if (_table == null)
                tableContext = new AssetTableContext();
            else
                tableContext = _table as AssetTableContext;

            tableContext.SetTableAggregation(aggregationCriteria);
            _table = tableContext;

            return _table;
        }

        public IBlockAssetTableContext SetTableColumnName(string columnName)
        {
            IAssetTableContext tableContext = null;
            if (_table == null)
                tableContext = new AssetTableContext();
            else
                tableContext = _table as AssetTableContext;

            tableContext.SetTableColumnName(columnName);
            _table = tableContext;

            return _table;
        }

        public IBlockAssetTableContext SetTableData(IEnumerable<IDictionary<string, object>> data)
        {
            IAssetTableContext tableContext = null;
            if (_table == null)
                tableContext = new AssetTableContext();
            else
                tableContext = _table as AssetTableContext;

            tableContext.SetTableData(data);
            _table = tableContext;

            return _table;
        }

        public IBlockAssetTableContext SetTableIds(IEnumerable<object> ids)
        {
            IAssetTableContext tableContext = null;
            if (_table == null)
                tableContext = new AssetTableContext();
            else
                tableContext = _table as AssetTableContext;

            tableContext.SetTableIds(ids);
            _table = tableContext;

            return _table;
        }

        public IBlockContext SetValue(int value)
        {
            _value = value;
            _dataType = DataTypeConstants.TYPE_INTEGER;
            return this;
        }

        public IBlockContext SetValue(bool value)
        {
            _value = value;
            _dataType = DataTypeConstants.TYPE_BOOLEAN;
            return this;
        }

        public IBlockContext SetValue(double value)
        {
            _value = value;
            _dataType = DataTypeConstants.TYPE_DOUBLE;
            return this;
        }

        public IBlockContext SetValue(string value)
        {
            _value = value;
            _dataType = DataTypeConstants.TYPE_TEXT;
            return this;
        }
        public IBlockContext SetValue(DateTime value)
        {
            _value = value;
            _dataType = DataTypeConstants.TYPE_TIMESTAMP;
            return this;
        }

        public IBlockContext SetValue(string dataType, object value)
        {
            switch (dataType)
            {
                case DataTypeConstants.TYPE_BOOLEAN:
                    return SetValue(value.ConvertToBoolean());
                case DataTypeConstants.TYPE_DOUBLE:
                    return SetValue(value.ConvertToNumber<double>());
                case DataTypeConstants.TYPE_INTEGER:
                    return SetValue(value.ConvertToNumber<int>());
                case DataTypeConstants.TYPE_TEXT:
                    return SetValue(Convert.ToString(value));
                default:
                    return SetValue(Convert.ToString(value));
            }
        }

        public IBlockContext SetValue(DateTime dateTime, int value)
        {
            _dateTime = dateTime;
            _value = value;
            _dataType = DataTypeConstants.TYPE_INTEGER;
            return this;
        }

        public IBlockContext SetValue(DateTime dateTime, bool value)
        {
            _dateTime = dateTime;
            _value = value;
            _dataType = DataTypeConstants.TYPE_BOOLEAN;
            return this;
        }

        public IBlockContext SetValue(DateTime dateTime, double value)
        {
            _dateTime = dateTime;
            _value = value;
            _dataType = DataTypeConstants.TYPE_DOUBLE;
            return this;
        }

        public IBlockContext SetValue(DateTime dateTime, string value)
        {
            _dateTime = dateTime;
            _value = value;
            _dataType = DataTypeConstants.TYPE_TEXT;
            return this;
        }
        public IBlockContext SetValue(params (DateTime dateTime, int value)[] values)
        {
            _value = values.Select(x => new BlockArrayValue(x.dateTime, x.value));
            _isArrayValue = true;
            _dataType = DataTypeConstants.TYPE_INTEGER;
            return this;
        }

        public IBlockContext SetValue(params (DateTime dateTime, bool value)[] values)
        {
            _value = values.Select(x => new BlockArrayValue(x.dateTime, x.value));
            _isArrayValue = true;
            _dataType = DataTypeConstants.TYPE_BOOLEAN;
            return this;
        }

        public IBlockContext SetValue(params (DateTime dateTime, double value)[] values)
        {
            _value = values.Select(x => new BlockArrayValue(x.dateTime, x.value));
            _isArrayValue = true;
            _dataType = DataTypeConstants.TYPE_DOUBLE;
            return this;
        }

        public IBlockContext SetValue(params (DateTime dateTime, string value)[] values)
        {
            _value = values.Select(x => new BlockArrayValue(x.dateTime, x.value));
            _isArrayValue = true;
            _dataType = DataTypeConstants.TYPE_TEXT;
            return this;
        }

        public void CopyFrom(IBlockContext source)
        {
            if (source != null)
            {
                this._assetId = source.AssetId;
                this._attribute = source.Attribute;
                this._table = source.Table;
                this._dataType = source.DataType;
                this._value = source.Value;
                this._isArrayValue = source.IsArrayValue;
                this._dateTime = source.DateTime;
            }
        }
    }
}