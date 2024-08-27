using System;
using System.Linq.Expressions;
using Device.Application.Asset;
using Device.Application.Constant;
using Newtonsoft.Json.Linq;

namespace Device.Application.AssetAttributeTemplate.Command.Model
{
    public class AttributeParsed
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string AttributeType { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }
        public string Expression { get; set; }
        public int? DecimalPlace { get; set; }
        public bool? ThousandSeparator { get; set; }
        public int? UomId { get; set; }
        public DateTime? UpdatedUtc { get; set; }
        public UomDto Uom { get; set; }
        public AttributeMapping Payload { get; set; }
        static Func<AttributeParsedDto, AttributeParsed> Converter = Projection.Compile();
        private static Expression<Func<AttributeParsedDto, AttributeParsed>> Projection
        {
            get
            {
                return attribute => new AttributeParsed
                {
                    Id = attribute.AttributeId,
                    Name = attribute.AttributeName,
                    AttributeType = attribute.AttributeType.ToLower(),
                    Value = attribute.Value,
                    DataType = attribute.DataType,
                    DecimalPlace = attribute.DecimalPlace,
                    ThousandSeparator = attribute.ThousandSeparator,
                    Payload = GetPayload(attribute),
                    UomId = attribute.UomId,
                    Expression = attribute.Expression,
                    Uom = attribute.UomDetail,
                    UpdatedUtc = attribute.UpdatedUtc
                };
            }
        }

        private static AttributeMapping GetPayload(AttributeParsedDto attributeParse)
        {
            if (string.Equals(attributeParse.AttributeType, AttributeTypeConstants.TYPE_INTEGRATION, StringComparison.InvariantCultureIgnoreCase))
            {
                return JObject.FromObject(new
                {
                    IntegrationMarkupName = attributeParse.ChannelMarkup,
                    IntegrationId = attributeParse.ChannelId,
                    DeviceMarkupName = attributeParse.DeviceMarkup,
                    DeviceId = attributeParse.DeviceTemplate,
                    MetricKey = attributeParse.Metric
                }).ToObject<AttributeMapping>();
            }
            if (string.Equals(attributeParse.AttributeType, AttributeTypeConstants.TYPE_DYNAMIC, StringComparison.InvariantCultureIgnoreCase))
            {
                return JObject.FromObject(new
                {
                    DeviceTemplate = attributeParse.DeviceTemplate,
                    MetricKey = attributeParse.Metric,
                    MetricName = attributeParse.MetricName,
                    MarkupName = attributeParse.DeviceMarkup,
                    DeviceTemplateId = attributeParse.DeviceTemplateId,
                }).ToObject<AttributeMapping>();
            }
            if (string.Equals(attributeParse.AttributeType, AttributeTypeConstants.TYPE_RUNTIME, StringComparison.InvariantCultureIgnoreCase))
            {
                return JObject.FromObject(new
                {
                    EnabledExpression = attributeParse.EnabledExpression,
                    Expression = attributeParse.Expression,
                    TriggerAttribute = attributeParse.TriggerAssetAttribute,
                    TriggerAttributeId = attributeParse.TriggerAssetAttributeId
                }).ToObject<AttributeMapping>();
            }
            if (string.Equals(attributeParse.AttributeType, AttributeTypeConstants.TYPE_COMMAND, StringComparison.InvariantCultureIgnoreCase))
            {
                return JObject.FromObject(new
                {
                    DeviceTemplate = attributeParse.DeviceTemplate,
                    DeviceTemplateId = attributeParse.DeviceTemplateId,
                    MarkupName = attributeParse.DeviceMarkup,
                    MetricKey = attributeParse.Metric
                }).ToObject<AttributeMapping>();
            }
            return null;
        }
        public static AttributeParsed Create(AttributeParsedDto attribute)
        {
            if (attribute != null)
                return Converter(attribute);
            return null;
        }
    }
    public class AttributeParsedDto
    {
        public Guid? AttributeId { get; set; }
        public string AttributeName { get; set; }
        public string AttributeType { get; set; }
        public string DeviceTemplate { get; set; }
        public string Channel { get; set; }
        public string ChannelMarkup { get; set; }
        public string DeviceMarkup { get; set; }
        public string Metric { get; set; }
        public string MetricName { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }
        public bool? EnabledExpression { get; set; }
        public string Expression { get; set; }
        public string Uom { get; set; }
        public int? DecimalPlace { get; set; }
        public bool? ThousandSeparator { get; set; }
        public string TriggerAssetAttribute { get; set; }
        public string TriggerAssetAttributeId { get; set; }
        public Guid? DeviceTemplateId { get; set; }
        public int? UomId { get; set; }
        public Guid? ChannelId { get; set; }
        public UomDto UomDetail { get; set; }
        public DateTime? UpdatedUtc { get; set; }
    }
    public class UomDto
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string Abbreviation { get; set; }
    }
}
