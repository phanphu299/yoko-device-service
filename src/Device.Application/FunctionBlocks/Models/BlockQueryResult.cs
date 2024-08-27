using System;
using Device.Application.Constant;

namespace Device.Application.BlockFunction.Model
{
    public class BlockQueryResult
    {
        public DateTime? Timestamp { get; set; }
        public object Value { get; set; }
        public string DataType { get; set; }
        public BlockQueryResult()
        {
        }
        public BlockQueryResult(object value, string dataType, DateTime? timestamp)
        {
            Value = value;
            DataType = dataType;
            Timestamp = timestamp;
        }
        public BlockQueryResult(double value)
        {
            Value = value;
            DataType = DataTypeConstants.TYPE_DOUBLE;
        }
        public BlockQueryResult(int value)
        {
            Value = value;
            DataType = DataTypeConstants.TYPE_INTEGER;
        }
        public BlockQueryResult(bool value)
        {
            Value = value;
            DataType = DataTypeConstants.TYPE_BOOLEAN;
        }
    }
}