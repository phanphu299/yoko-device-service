using System;
using System.Linq.Expressions;
using Device.Application.Asset;
using Device.Application.Uom.Command.Model;
using Device.ApplicationExtension.Extension;
using Newtonsoft.Json.Linq;

namespace Device.Application.AssetAttributeTemplate.Command.Model
{
    public class GetAssetAttributeTemplateDto : GetAssetAttributeTemplateSimplateDto
    {
        public AttributeMapping Payload { get; set; }
        static Func<Domain.Entity.AssetAttributeTemplate, GetAssetAttributeTemplateDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.AssetAttributeTemplate, GetAssetAttributeTemplateDto>> Projection
        {
            get
            {
                return entity => new GetAssetAttributeTemplateDto
                {
                    Id = entity.Id,
                    AssetTemplateId = entity.AssetTemplateId,
                    Name = entity.Name,
                    Value = entity.Value,
                    AttributeType = entity.AttributeType,
                    DataType = entity.DataType,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    UomId = entity.UomId,
                    Uom = GetSimpleUomDto.Create(entity.Uom),
                    Payload = GetPayload(entity),
                    ThousandSeparator = entity.ThousandSeparator,
                    DecimalPlace = entity.DecimalPlace,
                    SequentialNumber = entity.SequentialNumber
                };
            }
        }

        private static AttributeMapping GetPayload(Domain.Entity.AssetAttributeTemplate entity)
        {
            if (entity.AssetAttributeIntegration != null)
            {
                return JObject.FromObject(new
                {
                    entity.AssetAttributeIntegration.Id,
                    entity.AssetAttributeIntegration.IntegrationMarkupName,
                    entity.AssetAttributeIntegration.IntegrationId,
                    entity.AssetAttributeIntegration.DeviceMarkupName,
                    entity.AssetAttributeIntegration.DeviceId,
                    entity.AssetAttributeIntegration.MetricKey
                }).ToObject<AttributeMapping>();
            }
            if (entity.AssetAttributeDynamic != null)
            {
                return JObject.FromObject(new
                {
                    entity.AssetAttributeDynamic.Id,
                    entity.AssetAttributeDynamic.DeviceTemplateId,
                    entity.AssetAttributeDynamic.MetricKey,
                    entity.AssetAttributeDynamic.MarkupName
                }).ToObject<AttributeMapping>();
            }
            if (entity.AssetAttributeRuntime != null)
            {
                return JObject.FromObject(new
                {
                    entity.AssetAttributeRuntime.Id,
                    entity.AssetAttributeRuntime.EnabledExpression,
                    entity.AssetAttributeRuntime.Expression,
                    entity.AssetAttributeRuntime.ExpressionCompile,
                    //TriggerAssetTemplateId = entity.AssetTemplateId,
                    entity.AssetAttributeRuntime.TriggerAttributeId,
                    //entity.AssetAttributeRuntime.MarkupName
                }).ToObject<AttributeMapping>();
            }
            if (entity.AssetAttributeCommand != null)
            {
                return JObject.FromObject(new
                {
                    entity.AssetAttributeCommand.Id,
                    entity.AssetAttributeCommand.DeviceTemplateId,
                    entity.AssetAttributeCommand.MetricKey,
                    entity.AssetAttributeCommand.MarkupName
                }).ToObject<AttributeMapping>();
            }
            return null;
        }

        public new static GetAssetAttributeTemplateDto Create(Domain.Entity.AssetAttributeTemplate entity)
        {
            if (entity != null)
                return Converter(entity);
            return null;
        }
    }

    public class GetAssetAttributeTemplateSimplateDto
    {
        public Guid Id { get; set; }
        public Guid AssetTemplateId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string Expression { get; set; }
        public bool EnableExpression { get; set; }
        public string AttributeType { get; set; }
        public string DataType { get; set; }
        public string NormalizeName => Name.NormalizeAHIName();
        public int? UomId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public virtual GetSimpleUomDto Uom { get; set; }
        public int? DecimalPlace { get; set; }
        public bool? ThousandSeparator { get; set; }
        public int SequentialNumber { get; set; }
        static Func<Domain.Entity.AssetAttributeTemplate, GetAssetAttributeTemplateDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.AssetAttributeTemplate, GetAssetAttributeTemplateDto>> Projection
        {
            get
            {
                return entity => new GetAssetAttributeTemplateDto
                {
                    Id = entity.Id,
                    AssetTemplateId = entity.AssetTemplateId,
                    Name = entity.Name,
                    Value = entity.Value,
                    // Expression = entity.Expression,
                    // EnableExpression = entity.EnabledExpression,
                    AttributeType = entity.AttributeType,
                    DataType = entity.DataType,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    UomId = entity.UomId,
                    Uom = GetSimpleUomDto.Create(entity.Uom),
                    ThousandSeparator = entity.ThousandSeparator,
                    DecimalPlace = entity.DecimalPlace,
                    SequentialNumber = entity.SequentialNumber
                };
            }
        }

        public static GetAssetAttributeTemplateDto Create(Domain.Entity.AssetAttributeTemplate entity)
        {
            if (entity != null)
                return Converter(entity);
            return null;
        }
    }
}
