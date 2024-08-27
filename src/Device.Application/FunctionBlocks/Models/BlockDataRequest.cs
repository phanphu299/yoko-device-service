using System;
using Device.Application.Constant;

namespace Device.Application.BlockFunction.Model
{
    public class BlockDataRequest
    {
        public DateTime Timestamp { get; set; }
        public object Value { get; set; }
        private string _dataType;
        public string DataType
        {
            get
            {
                if (!string.IsNullOrEmpty(_dataType))
                {
                    return _dataType;
                }
                else if (Value is bool)
                {
                    return DataTypeConstants.TYPE_BOOLEAN;
                }
                else if (Value is int)
                {
                    return DataTypeConstants.TYPE_INTEGER;
                }
                else if (Value is long)
                {
                    return DataTypeConstants.TYPE_DOUBLE;
                }
                else if (Value is double)
                {
                    return DataTypeConstants.TYPE_DOUBLE;
                }
                else if (Value is float)
                {
                    return DataTypeConstants.TYPE_DOUBLE;
                }
                return DataTypeConstants.TYPE_TEXT;
            }
        }
        public bool IsArrayValue { get; set; }
        public BlockDataRequest()
        {
        }
        public BlockDataRequest(object value, string dataType, DateTime timestamp)
        {
            Value = value;
            _dataType = dataType;
            Timestamp = timestamp;
        }
        public BlockDataRequest(object value, DateTime timestamp)
        {
            Value = value;
            Timestamp = timestamp;
        }
        public BlockDataRequest(object value, string dataType, bool isArrayValue)
        {
            Value = value;
            _dataType = dataType;
            IsArrayValue = isArrayValue;
        }
        public BlockDataRequest(double value)
        {
            Value = value;
            _dataType = DataTypeConstants.TYPE_DOUBLE;
        }
        public BlockDataRequest(int value)
        {
            Value = value;
            _dataType = DataTypeConstants.TYPE_INTEGER;
        }
        public BlockDataRequest(bool value)
        {
            Value = value;
            _dataType = DataTypeConstants.TYPE_BOOLEAN;
        }
    }
}