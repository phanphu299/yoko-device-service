using System;
using System.Collections.Generic;
using AHI.Device.Function.Constant;

namespace AHI.Device.Function.Model.ExportModel
{
    public class AssetAttribute
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        public IList<AttributeModel> Attributes { get; set; }

        AssetAttribute()
        {
            Attributes = new List<AttributeModel>();
        }
    }

    public class AttributeModel
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public string AttributeName { get; set; }
        public string Type { get; set; }
        public string DeviceId { get; set; }
        public Guid? ChannelId { get; set; }
        public string Channel { get; set; }
        public string Metric { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }
        public Guid? AliasAsset { get; set; }
        public string AliasAttribute { get; set; }
        public string TriggerAttribute { get; set; }
        public bool? EnabledExpression { get; set; }
        public string Expression { get; set; }
        public string Uom { get; set; }
        public int? DecimalPlace { get; set; }
        public bool? ThousandSeparator { get; set; }
        public DateTime CreatedUtc { get; set; }
        public int SequentialNumber { get; set; }
        public bool IsRuntimeAttribute => Type == AttributeTypeConstants.TYPE_RUNTIME;
        public bool IsIntegrationAttribute => Type == AttributeTypeConstants.TYPE_INTEGRATION;
    }
}
