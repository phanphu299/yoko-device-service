using System;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.Constant;
using Device.Application.Uom.Command.Model;
using Device.ApplicationExtension.Extension;
using Device.Domain.Entity;
using Newtonsoft.Json.Linq;

namespace Device.Application.Asset.Command.Model
{
    public class AssetAttributeDto : IAssetAttribute
    {
        static Func<Domain.Entity.AssetAttribute, AssetAttributeDto> AttributeConverter = Projection.Compile();
        static Func<GetAssetDto, AssetAttributeDto, Domain.Entity.HistoricalEntity> HistoricalEntityConverter = ProjectionEntity.Compile();
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public string Name { get; set; }
        public object Value { get; set; }
        public string AttributeType { get; set; }
        public string DataType { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public bool Deleted { set; get; }
        public int? UomId { get; set; }
        public string NormalizeName => Name.NormalizeAHIName();
        public GetSimpleUomDto Uom { get; set; }
        public AttributeMapping Payload { get; set; }
        public int? DecimalPlace { get; set; }
        public bool? ThousandSeparator { get; set; }
        public int SequentialNumber { get; set; }
        private static Expression<Func<Domain.Entity.AssetAttribute, AssetAttributeDto>> Projection
        {
            get
            {
                return entity => new AssetAttributeDto
                {
                    Id = entity.Id,
                    AssetId = entity.AssetId,
                    Name = entity.Name,
                    Value = entity.Value,
                    AttributeType = entity.AttributeType,
                    DataType = entity.DataType,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    UomId = entity.UomId,
                    Uom = GetSimpleUomDto.Create(entity.Uom),
                    Payload = GetAssetAttributePayload(entity),
                    DecimalPlace = entity.DecimalPlace,
                    ThousandSeparator = entity.ThousandSeparator,
                    SequentialNumber = entity.SequentialNumber
                };
            }
        }
        private static Expression<Func<GetAssetDto, AssetAttributeDto, Domain.Entity.HistoricalEntity>> ProjectionEntity
        {
            get
            {
                return (asset, attribute) => CreateHistoricalEntityFromAssetAttribute(asset, attribute);
            }
        }

        public static AssetAttributeDto Create(Domain.Entity.AssetAttribute entity)
        {
            if (entity == null)
                return null;
            return AttributeConverter(entity);
        }
        public static HistoricalEntity CreateHistoricalEntity(GetAssetDto assetDto, AssetAttributeDto entity)
        {
            if (entity == null)
                return null;
            return HistoricalEntityConverter(assetDto, entity);
        }

        private static AttributeMapping GetAssetAttributePayload(Domain.Entity.AssetAttribute entity)
        {
            if (entity.AssetAttributeDynamic != null)
            {
                return JObject.FromObject(
                    new
                    {
                        Id = entity.AssetAttributeDynamic.Id,
                        MetricKey = entity.AssetAttributeDynamic.MetricKey,
                        DeviceId = entity.AssetAttributeDynamic.DeviceId,
                    })
                    .ToObject<AttributeMapping>();
            }
            if (entity.AssetAttributeIntegration != null)
            {
                return JObject.FromObject(new
                {
                    Id = entity.AssetAttributeIntegration.Id,
                    IntegrationId = entity.AssetAttributeIntegration.IntegrationId,
                    DeviceId = entity.AssetAttributeIntegration.DeviceId,
                    MetricKey = entity.AssetAttributeIntegration.MetricKey
                }).ToObject<AttributeMapping>();
            }

            if (entity.AssetAttributeAlias != null)
            {
                return JObject.FromObject(new
                {
                    Id = entity.AssetAttributeAlias.Id,
                    AliasAssetId = entity.AssetAttributeAlias.AliasAssetId,
                    AliasAttributeId = entity.AssetAttributeAlias.AliasAttributeId,
                    AliasAssetName = entity.AssetAttributeAlias.AliasAssetName,
                    AliasAttributeName = entity.AssetAttributeAlias.AliasAttributeName,
                }).ToObject<AttributeMapping>();
            }

            if (entity.AssetAttributeRuntime != null)
            {
                Guid? triggerAssetId = null;
                Guid? triggerAttribteId = null;
                bool? hasTriggerError = null;
                if (entity.AssetAttributeRuntime.IsTriggerVisibility)
                {
                    var triggerAssetAttribute = entity.AssetAttributeRuntime.Triggers.Where(x => x.IsSelected).SingleOrDefault();
                    if (triggerAssetAttribute == null)
                    {
                        hasTriggerError = true;
                    }
                    else
                    {
                        // single trigger
                        triggerAssetId = triggerAssetAttribute.TriggerAssetId;
                        triggerAttribteId = triggerAssetAttribute.TriggerAttributeId;
                    }
                }
                return JObject.FromObject(new
                {
                    Id = entity.AssetAttributeRuntime.Id,
                    entity.AssetAttributeRuntime.EnabledExpression,
                    entity.AssetAttributeRuntime.Expression,
                    entity.AssetAttributeRuntime.ExpressionCompile,
                    TriggerAssetId = triggerAssetId,
                    TriggerAttributeId = triggerAttribteId,
                    hasTriggerError = hasTriggerError
                }).ToObject<AttributeMapping>();
            }

            if (entity.AssetAttributeCommand != null)
            {
                return JObject.FromObject(new
                {
                    Id = entity.AssetAttributeCommand.Id,
                    entity.AssetAttributeCommand.DeviceId,
                    entity.AssetAttributeCommand.MetricKey,
                    entity.AssetAttributeCommand.Value,
                    entity.AssetAttributeCommand.RowVersion,
                    //az https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/75526
                    DataType = entity.AssetAttributeCommand.Device?.Template?.Bindings?.First(x => x.Key == entity.AssetAttributeCommand.MetricKey).DataType
                }).ToObject<AttributeMapping>();
            }
            return null;
        }

        private static HistoricalEntity CreateHistoricalEntityFromAssetAttribute(GetAssetDto asset, AssetAttributeDto attribute)
        {
            return new Domain.Entity.HistoricalEntity
            {
                AssetId = attribute.AssetId,
                AssetName = asset.Name,
                AssetNameNormalize = asset.NormalizeName,
                AttributeId = attribute.Id,
                AttributeName = attribute.Name,
                AttributeNameNormalize = attribute.NormalizeName,
                UomId = attribute.UomId,
                UomName = attribute.Uom != null ? attribute.Uom.Name : null,
                UomAbbreviation = attribute.Uom != null ? attribute.Uom.Abbreviation : null,
                IntegrationId = attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.INTEGRATION_ID) && attribute.Payload[PayloadConstants.INTEGRATION_ID] != null ? Guid.Parse(attribute.Payload[PayloadConstants.INTEGRATION_ID].ToString()) : (Guid?)null,
                DataType = (attribute.DataType == null && attribute.Payload != null && attribute.Payload.DataType != null) ? attribute.Payload.DataType : attribute.DataType,
                DeviceId = attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.DEVICE_ID) && attribute.Payload[PayloadConstants.DEVICE_ID] != null ? attribute.Payload[PayloadConstants.DEVICE_ID].ToString() : null,
                MetricKey = attribute.Payload != null && attribute.Payload.ContainsKey(PayloadConstants.METRIC_KEY) && attribute.Payload[PayloadConstants.METRIC_KEY] != null ? attribute.Payload[PayloadConstants.METRIC_KEY].ToString() : null,
                AttributeType = attribute.AttributeType,
                DecimalPlace = (attribute.DecimalPlace == null && attribute.Payload != null && attribute.Payload.DecimalPlace != null) ? attribute.Payload.DecimalPlace : attribute.DecimalPlace,
                ThousandSeparator = (attribute.ThousandSeparator == null && attribute.Payload != null && attribute.Payload.ThousandSeparator != null) ? attribute.Payload.ThousandSeparator : attribute.ThousandSeparator
            };
        }
    }
}