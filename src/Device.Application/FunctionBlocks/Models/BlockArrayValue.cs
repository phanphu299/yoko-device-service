using System;

namespace Device.Application.Asset.Command.Model
{
    public class BlockArrayValue
    {
        public DateTime DateTime { get; set; }
        public object Value { get; set; }

        public BlockArrayValue(DateTime dateTime, object value)
        {
            DateTime = dateTime;
            Value = value;
        }
    }
}