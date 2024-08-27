using System;
using System.Collections.Generic;

namespace Device.Application.Asset.Command.Model
{
    public class AssetAttributeParsedDto
    {
        public IEnumerable<AttributeParsedDto> Attributes { get; set; }
        public List<ErrorDetail> Errors { get; set; }

        public AssetAttributeParsedDto()
        {
            Attributes = new List<AttributeParsedDto>();
            Errors = new List<ErrorDetail>();
        }
    }

    public class AttributeParsedDto
    {
        public Guid? Id { get; set; }
        public string AttributeName { get; set; }
        public string AttributeType { get; set; }
        public string DeviceId { get; set; }
        public Guid? ChannelId { get; set; }
        public string Channel { get; set; }
        public string Metric { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }
        public string AliasAsset { get; set; }
        public string AliasAssetName { get; set; }
        public string AliasAttribute { get; set; }
        public string AliasAttributeId { get; set; }
        public string TriggerAttribute { get; set; }
        public Guid? TriggerAttributeId { get; set; }
        public bool? EnabledExpression { get; set; }
        public string Expression { get; set; }
        public string Uom { get; set; }
        public int? UomId { get; set; }
        public object UomData { get; set; }
        public int? DecimalPlace { get; set; }
        public bool? ThousandSeparator { get; set; }
        public DateTime? UpdatedUtc { get; set; }
    }

    public class ErrorDetail
    {
        public string Detail { get; set; }
        public string Column { get; set; }
        public string Row { get; set; }
        public string Type { get; set; }
        public string ColumnName { get; set; }
    }
}
