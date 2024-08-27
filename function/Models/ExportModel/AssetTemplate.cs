using System;
using System.Collections.Generic;

namespace AHI.Device.Function.Model.ExportModel
{
    public class AssetTemplate
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public IDictionary<string, string> Tags = new Dictionary<string, string>();
        public IEnumerable<long> TagIds { get; set; } = new List<long>();
        public ICollection<AssetTemplateAttribute> Attributes { get; set; }

        public int Ordinal { get; set; }
    }

    public class AssetTemplateTagEntity
    {
        public Guid Id { get; set; }
        public long? TagId { get; set; }
    }

    public class AssetTemplateAttribute
    {
        public AssetTemplateAttribute()
        {
        }
        public Guid Id { get; set; }
        public string AttributeName { get; set; }
        public string Type { get; set; }
        public string DeviceTemplate { get; set; }
        public Guid? ChannelId { get; set; }
        public string Channel { get; set; }
        public string ChannelMarkup { get; set; }
        public string Device { get; set; }
        public string DeviceMarkup { get; set; }
        public string Metric { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }
        public string Expression { get; set; }
        public string Uom { get; set; }
        public int? DecimalPlace { get; set; }
        public bool? ThousandSeparator { get; set; }
        public string TriggerAssetAttribute { get; set; }
        public bool? EnabledExpression { get; set; }
    }
}
