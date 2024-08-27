using System;

namespace Device.Domain.Entity
{
    public class HistoricalEntity
    {
        public Guid AssetId { get; set; }
        public string AssetNameNormalize { get; set; }
        public Guid AttributeId { get; set; }
        public Guid? IntegrationId { get; set; }
        public string MetricKey { get; set; }
        public string DeviceId { get; set; }
        public string DataType { get; set; }
        public string AttributeType { get; set; }
        public string AssetName { get; set; }
        public string AttributeName { get; set; }
        public string AttributeNameNormalize { get; set; }
        public int? UomId { get; set; }
        public string UomName { get; set; }
        public string UomAbbreviation { get; set; }
        public int? DecimalPlace { get; set; }
        public bool? ThousandSeparator { get; set; }
    }
}
