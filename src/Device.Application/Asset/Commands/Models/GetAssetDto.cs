using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.Uom.Command.Model;
using AHI.Infrastructure.Service.Tag.Extension;
using Newtonsoft.Json.Linq;
using AHI.Infrastructure.Service.Tag.Model;
using Device.ApplicationExtension.Extension;

namespace Device.Application.Asset.Command.Model
{
    public abstract class BaseAssetDto : TagDtos
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string NormalizeName => Name.NormalizeAHIName();
        public int RetentionDays { get; set; }
        public Guid? ParentAssetId { get; set; }
        public Guid? AssetTemplateId { get; set; }
        public string AssetTemplateName { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public string CurrentUserUpn { set; get; }
        public DateTime? CurrentTimestamp { set; get; }
        public string RequestLockUserUpn { set; get; }
        public DateTime? RequestLockTimestamp { set; get; }
        public DateTime? RequestLockTimeout { set; get; }
        public GetAssetSimpleDto Parent { get; set; }
        public IEnumerable<AssetAttributeDto> Attributes { get; set; } = new List<AssetAttributeDto>();
        public bool HasWarning { get; set; }
        public string ResourcePath { get; set; }
        public string CreatedBy { get; set; }
        public bool IsDocument { get; set; }

        protected static IEnumerable<AssetAttributeDto> GetAttribute(Domain.Entity.Asset dto)
        {
            var attributes = new List<AssetAttributeDto>();
            if (dto.AssetTemplateId != null && dto.AssetTemplate != null && dto.AssetTemplate.Attributes.Any())
            {
                // FE need to call the snapshot api to the value
                // get snapshot value
                var mappingAttributes = dto.AssetTemplate.Attributes.OrderBy(x => x.CreatedUtc).ThenBy(x => x.SequentialNumber).Select(entity =>
                {
                    var attributePayload = GetAssetAttributePayload(dto, entity);
                    if (attributePayload != default)
                    {
                        var (assetAttributeId, payload) = attributePayload;
                        return CreateAttributeFromTemplateMapping(assetAttributeId, payload, dto, entity);
                    }
                    else
                    {
                        return null;
                    }
                }).Where(x => x != null);
                attributes.AddRange(mappingAttributes);
            }

            attributes.AddRange(dto.Attributes.Select(AssetAttributeDto.Create));
            return attributes.OrderBy(x => x.CreatedUtc).ThenBy(x => x.SequentialNumber);

        }

        private static (Guid, AttributeMapping) GetAssetAttributePayload(Domain.Entity.Asset dto, Domain.Entity.AssetAttributeTemplate entity)
        {
            var assetDynamicMapping = dto.AssetAttributeDynamicMappings.FirstOrDefault(x => x.AssetAttributeTemplateId == entity.Id);
            if (assetDynamicMapping != null)
            {
                return (assetDynamicMapping.Id, JObject.FromObject(new
                {
                    TemplateAttributeId = assetDynamicMapping.AssetAttributeTemplateId,
                    assetDynamicMapping.DeviceId,
                    entity.AssetAttributeDynamic.MetricKey,
                    entity.AssetAttributeDynamic.MarkupName
                }).ToObject<AttributeMapping>());
            }

            var assetIntegrationMapping = dto.AssetAttributeIntegrationMappings.FirstOrDefault(x => x.AssetAttributeTemplateId == entity.Id);
            if (assetIntegrationMapping != null)
            {
                return (assetIntegrationMapping.Id, JObject.FromObject(new
                {
                    TemplateAttributeId = assetIntegrationMapping.AssetAttributeTemplateId,
                    assetIntegrationMapping.IntegrationId,
                    assetIntegrationMapping.DeviceId,
                    entity.AssetAttributeIntegration.MetricKey,
                    entity.AssetAttributeIntegration.DeviceMarkupName,
                    entity.AssetAttributeIntegration.IntegrationMarkupName
                }).ToObject<AttributeMapping>());
            }

            var assetRuntimeMapping = dto.AssetAttributeRuntimeMappings.FirstOrDefault(x => x.AssetAttributeTemplateId == entity.Id);
            if (assetRuntimeMapping != null)
            {
                Guid? triggerAttributeId = null;
                bool? hasTriggerError = null;
                if (assetRuntimeMapping.IsTriggerVisibility)
                {
                    var triggerAssetAttribute = assetRuntimeMapping.Triggers.Where(x => x.IsSelected).SingleOrDefault();

                    if (triggerAssetAttribute == null)
                    {
                        hasTriggerError = true;
                    }
                    else
                    {
                        triggerAttributeId = triggerAssetAttribute.TriggerAttributeId;
                    }
                }
                return (assetRuntimeMapping.Id, JObject.FromObject(new
                {
                    TemplateAttributeId = assetRuntimeMapping.AssetAttributeTemplateId,
                    assetRuntimeMapping.Expression,
                    assetRuntimeMapping.ExpressionCompile,
                    hasTriggerError = hasTriggerError,
                    TriggerAttributeId = triggerAttributeId,
                    assetRuntimeMapping.EnabledExpression
                }).ToObject<AttributeMapping>());
            }

            var assetStaticMapping = dto.AssetAttributeStaticMappings.FirstOrDefault(x => x.AssetAttributeTemplateId == entity.Id);
            if (assetStaticMapping != null)
            {
                return (assetStaticMapping.Id, JObject.FromObject(new
                {
                    TemplateAttributeId = assetStaticMapping.AssetAttributeTemplateId,
                    assetStaticMapping.Value
                }).ToObject<AttributeMapping>());
            }

            var assetCommandMapping = dto.AssetAttributeCommandMappings.FirstOrDefault(x => x.AssetAttributeTemplateId == entity.Id);
            if (assetCommandMapping != null)
            {
                return (assetCommandMapping.Id, JObject.FromObject(new
                {
                    TemplateAttributeId = assetCommandMapping.AssetAttributeTemplateId,
                    assetCommandMapping.DeviceId,
                    entity.AssetAttributeCommand.MetricKey,
                    entity.AssetAttributeCommand.MarkupName,
                    assetCommandMapping.Value,
                    assetCommandMapping.RowVersion,
                }).ToObject<AttributeMapping>());
            }

            var assetAliasMapping = dto.AssetAttributeAliasMappings.FirstOrDefault(x => x.AssetAttributeTemplateId == entity.Id);
            if (assetAliasMapping != null)
            {
                return (assetAliasMapping.Id, JObject.FromObject(new
                {
                    TemplateAttributeId = assetAliasMapping.AssetAttributeTemplateId,
                    AliasAssetId = assetAliasMapping.AliasAssetId,
                    AliasAssetName = assetAliasMapping.AliasAssetName,
                    AliasAttributeId = assetAliasMapping.AliasAttributeId,
                    AliasAttributeName = assetAliasMapping.AliasAttributeName,
                    DataType = assetAliasMapping.DataType,
                    UomId = assetAliasMapping.UomId,
                    DecimalPlace = assetAliasMapping.DecimalPlace,
                    ThousandSeparator = assetAliasMapping.ThousandSeparator
                }).ToObject<AttributeMapping>());
            }

            return default;
        }

        private static AssetAttributeDto CreateAttributeFromTemplateMapping(Guid assetAttributeId, AttributeMapping payload, Domain.Entity.Asset dto, Domain.Entity.AssetAttributeTemplate entity)
        {
            return new AssetAttributeDto()
            {
                Id = assetAttributeId,
                AssetId = dto.Id,
                Name = entity.Name,
                AttributeType = entity.AttributeType,
                DataType = (entity.DataType == null && payload != null && payload.DataType != null) ? payload.DataType : entity.DataType,
                CreatedUtc = entity.CreatedUtc,
                UpdatedUtc = entity.UpdatedUtc,
                UomId = entity.UomId,
                Uom = GetSimpleUomDto.Create(entity.Uom),
                Payload = payload,
                DecimalPlace = (entity.DecimalPlace == null && payload != null && payload.DecimalPlace != null) ? payload.DecimalPlace : entity.DecimalPlace,
                ThousandSeparator = (entity.ThousandSeparator == null && payload != null && payload.ThousandSeparator != null) ? payload.ThousandSeparator : entity.ThousandSeparator
            };
        }
    }

    public class GetAssetDto : BaseAssetDto
    {
        static Func<Domain.Entity.Asset, GetAssetDto> Converter = Projection.Compile();

        public IEnumerable<GetAssetSimpleDto> Children { get; set; } = new List<GetAssetSimpleDto>();

        private static Expression<Func<Domain.Entity.Asset, GetAssetDto>> Projection
        {
            get
            {
                return entity => new GetAssetDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    RetentionDays = entity.RetentionDays,
                    ParentAssetId = entity.ParentAssetId,
                    AssetTemplateId = entity.AssetTemplateId,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    Parent = GetAssetSimpleDto.Create(entity.ParentAsset),
                    Attributes = GetAttribute(entity),
                    Children = entity.Children.Select(GetAssetSimpleDto.Create),
                    HasWarning = entity.AssetWarning != null && entity.AssetWarning.HasWarning,
                    ResourcePath = entity.ResourcePath,
                    CreatedBy = entity.CreatedBy,
                    IsDocument = entity.IsDocument,
                    AssetTemplateName = entity.AssetTemplate == null ? null : entity.AssetTemplate.Name,
                    Tags = entity.EntityTags.MappingTagDto()
                };
            }
        }

        public static GetAssetDto Create(Domain.Entity.Asset entity)
        {
            if (entity == null)
                return null;
            return Converter(entity);
        }
    }

    public class GetFullAssetDto : BaseAssetDto
    {
        static Func<Domain.Entity.Asset, GetFullAssetDto> Converter = Projection.Compile();
        public ICollection<GetFullChildrenAssetDto> Children { get; set; }

        private static Expression<Func<Domain.Entity.Asset, GetFullAssetDto>> Projection
        {
            get
            {
                return entity => new GetFullAssetDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    RetentionDays = entity.RetentionDays,
                    ParentAssetId = entity.ParentAssetId,
                    AssetTemplateId = entity.AssetTemplateId,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    Parent = GetAssetSimpleDto.Create(entity.ParentAsset),
                    Attributes = GetAttribute(entity),
                    Children = entity.Children.Select(GetFullChildrenAssetDto.Create).ToList(),
                    HasWarning = entity.AssetWarning != null && entity.AssetWarning.HasWarning,
                    ResourcePath = entity.ResourcePath,
                    CreatedBy = entity.CreatedBy,
                    IsDocument = entity.IsDocument,
                    AssetTemplateName = entity.AssetTemplate == null ? null : entity.AssetTemplate.Name,
                    Tags = entity.EntityTags.MappingTagDto()
                };
            }
        }

        public static GetFullAssetDto Create(Domain.Entity.Asset entity)
        {
            if (entity == null)
                return null;
            return Converter(entity);
        }
    }

    public class GetFullChildrenAssetDto : GetFullAssetDto
    {
        static Func<Domain.Entity.Asset, GetFullChildrenAssetDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.Asset, GetFullChildrenAssetDto>> Projection
        {
            get
            {
                return entity => new GetFullChildrenAssetDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    RetentionDays = entity.RetentionDays,
                    ParentAssetId = entity.ParentAssetId,
                    AssetTemplateId = entity.AssetTemplateId,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    Attributes = GetAttribute(entity),
                    Children = entity.Children.Select(Create).ToList(),
                    HasWarning = entity.AssetWarning != null && entity.AssetWarning.HasWarning,
                    ResourcePath = entity.ResourcePath,
                    CreatedBy = entity.CreatedBy,
                    IsDocument = entity.IsDocument,
                    AssetTemplateName = entity.AssetTemplate == null ? null : entity.AssetTemplate.Name,
                    Tags = entity.EntityTags.MappingTagDto()
                };
            }
        }
        public static new GetFullChildrenAssetDto Create(Domain.Entity.Asset entity)
        {
            if (entity == null)
                return null;
            return Converter(entity);
        }
    }
}
