using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Device.Application.Asset.Command.Model
{
    public class AssetAttributeParsedResponse
    {
        public IEnumerable<AssetAttributeParsed> Attributes { get; set; }
        public List<ErrorDetail> Errors { get; set; }

        public AssetAttributeParsedResponse()
        {
            Attributes = new List<AssetAttributeParsed>();
            Errors = new List<ErrorDetail>();
        }
    }

    public class AssetAttributeParsed
    {
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public string AttributeType { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }
        public object Uom { get; set; }
        public int? UomId { get; set; }
        public int? DecimalPlace { get; set; }
        public bool? ThousandSeparator { get; set; }
        public DateTime? UpdatedUtc { get; set; }
        public dynamic Payload { get; set; }
        static Func<AttributeParsedDto, AssetAttributeParsed> Converter = Projection.Compile();
        private static Expression<Func<AttributeParsedDto, AssetAttributeParsed>> Projection
        {
            get
            {
                return (model) => new AssetAttributeParsed
                {
                    Id = model.Id,
                    Name = model.AttributeName,
                    AttributeType = model.AttributeType,
                    Value = model.Value,
                    DataType = model.DataType,
                    Uom = model.UomData,
                    UomId = model.UomId,
                    DecimalPlace = model.DecimalPlace,
                    ThousandSeparator = model.ThousandSeparator,
                    UpdatedUtc = model.UpdatedUtc,
                    Payload = new
                    {
                        DeviceId = model.DeviceId,
                        MetricKey = model.Metric,
                        IntegrationId = model.ChannelId,
                        AliasAssetId = model.AliasAsset,
                        AliasAssetName = model.AliasAssetName,
                        AliasAttributeId = model.AliasAttributeId,
                        AliasAttributeName = model.AliasAttribute,
                        EnabledExpression = model.EnabledExpression,
                        Expression = model.Expression,
                        TriggerAttribute = model.TriggerAttribute,
                        TriggerAttributeId = model.TriggerAttributeId
                    }
                };
            }
        }

        public static AssetAttributeParsed Create(AttributeParsedDto model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
