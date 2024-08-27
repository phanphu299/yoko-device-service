using System;

namespace Device.Job.Model
{
    public class FlattenHistoricalData
    {
        public string AssetId { get; set; }
        public Guid AttributeId { get; set; }
        public string AttributeName { get; set; }
        public long UnixTimestamp { get; set; }
        public object Value { get; set; }

        public FlattenHistoricalData(string assetId, Guid attributeId, string attributeName, long unixTimestamp, object v)
        {
            AssetId = assetId;
            AttributeId = attributeId;
            AttributeName = attributeName;
            UnixTimestamp = unixTimestamp;
            Value = v;
        }
    }
}