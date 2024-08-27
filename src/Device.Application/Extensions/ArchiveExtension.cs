using System;
using Device.Application.Asset;
using Device.Application.Asset.Command.Model;
using Device.Application.AssetAttributeTemplate.Command.Model;
using Newtonsoft.Json.Linq;
using static Device.Application.Constant.AttributeTypeConstants;

namespace Device.ApplicationExtension.Extension
{
    public static class AttributeMappingExtension
    {
        public static bool TryGetByKey(this AttributeMapping mapping, string key, string alternateKey, out object value)
        {
            if (mapping != null)
            {
                if (!string.IsNullOrEmpty(key) && mapping.ContainsKey(key))
                {
                    value = mapping[key];
                    return value != null; // must return false if key exists but value is null anyway
                }

                if (!string.IsNullOrEmpty(alternateKey) && mapping.ContainsKey(alternateKey))
                {
                    value = mapping[alternateKey];
                    return value != null; // must return false if key exists but value is null anyway
                }
            }

            value = default;
            return false;
        }

        public static string GetStringByKey(this AttributeMapping mapping, string key, string alternateKey = null)
        {
            if (mapping.TryGetByKey(key, alternateKey, out var str))
                return str as string;

            return null;
        }

        public static bool GetBoolByKey(this AttributeMapping mapping, string key, string alternateKey = null)
        {
            if (mapping.TryGetByKey(key, alternateKey, out var b) && b != null)
                return Convert.ToBoolean(b);

            return false;
        }

        public static Guid GetIdByKey(this AttributeMapping mapping, string key, string alternateKey = null)
        {
            if (mapping.TryGetByKey(key, alternateKey, out var id) && id != null)
                return Guid.Parse(id as string);

            return Guid.Empty;
        }

        public static Guid? GetNullableIdByKey(this AttributeMapping mapping, string key, string alternateKey = null)
        {
            if (mapping.TryGetByKey(key, alternateKey, out var id) && id != null)
                return Guid.Parse(id as string);

            return null;
        }

        public static Guid GetId(this AttributeMapping mapping) => mapping.GetIdByKey("id", "Id");
        public static string GetAttributeType(this AttributeMapping mapping) => mapping.GetStringByKey("attributeType", "AttributeType");
        public static Guid GetAssetAttributeTemplateId(this AttributeMapping mapping) => mapping.GetIdByKey("assetAttributeTemplateId", "AssetAttributeTemplateId");
        public static Guid GetAssetId(this AttributeMapping mapping) => mapping.GetIdByKey("assetId", "AssetId");
        public static Guid GetAttributeId(this AttributeMapping mapping) => mapping.GetIdByKey("attributeId", "AttributeId");
        public static string GetExpression(this AttributeMapping mapping) => mapping.GetStringByKey("expression", "Expression");
        public static string GetExpressionCompile(this AttributeMapping mapping) => mapping.GetStringByKey("expressionCompile", "ExpressionCompile");
        public static Guid GetTriggerAssetId(this AttributeMapping mapping) => mapping.GetIdByKey("triggerAssetId", "TriggerAssetId");
        public static Guid GetTriggerAttributeId(this AttributeMapping mapping) => mapping.GetIdByKey("triggerAttributeId", "TriggerAttributeId");
        public static Guid? GetAliasAssetId(this AttributeMapping mapping) => mapping.GetNullableIdByKey("aliasAssetId", "AliasAssetId");
        public static Guid? GetAliasAttributeId(this AttributeMapping mapping) => mapping.GetNullableIdByKey("aliasAttributeId", "AliasAttributeId");
        public static int? GetUomId(this AttributeMapping mapping) => mapping.TryGetByKey("uomId", "UomId", out var uomId) ? Convert.ToInt32(uomId) : (int?)null;
        public static int GetSequentialNumber(this AttributeMapping mapping) => mapping.TryGetByKey("sequentialNumber", "SequentialNumber", out var seq) ? Convert.ToInt32(seq) : 1;
        public static string GetMarkupName(this AttributeMapping mapping) => mapping.GetStringByKey("markupName", "MarkupName");
        public static string GetIntegrationMarkupName(this AttributeMapping mapping) => mapping.GetStringByKey("integrationMarkupName", "IntegrationMarkupName");
        public static string GetDeviceMarkupName(this AttributeMapping mapping) => mapping.GetStringByKey("deviceMarkupName", "DeviceMarkupName");
        public static Guid GetDeviceTemplateId(this AttributeMapping mapping) => mapping.GetIdByKey("deviceTemplateId", "DeviceTemplateId");
    }

    public static class ArchiveExtension
    {
        public static AttributeMapping CreateDto(Domain.Entity.AssetAttributeStaticMapping mapping)
        {
            return JObject.FromObject(new
            {
                AttributeType = TYPE_STATIC,
                mapping.Id,
                mapping.AssetId,
                mapping.AssetAttributeTemplateId,
                mapping.Value,
                mapping.IsOverridden,
                mapping.SequentialNumber
            }).ToObject<AttributeMapping>();
        }

        public static Domain.Entity.AssetAttributeStaticMapping CreateStaticMapping(AttributeMapping mapping)
        {
            return new Domain.Entity.AssetAttributeStaticMapping()
            {
                Id = mapping.GetId(),
                AssetId = mapping.GetAssetId(),
                AssetAttributeTemplateId = mapping.GetAssetAttributeTemplateId(),
                Value = mapping.GetStringByKey("value", "Value"),
                IsOverridden = mapping.GetBoolByKey("isOverridden", "IsOverridden"),
                SequentialNumber = mapping.GetSequentialNumber()
            };
        }

        public static AttributeMapping CreateDto(Domain.Entity.AssetAttributeDynamicMapping mapping)
        {
            return JObject.FromObject(new
            {
                AttributeType = TYPE_DYNAMIC,
                mapping.Id,
                mapping.AssetId,
                mapping.AssetAttributeTemplateId,
                mapping.DeviceId,
                mapping.MetricKey,
                mapping.SequentialNumber
            }).ToObject<AttributeMapping>();
        }

        public static Domain.Entity.AssetAttributeDynamicMapping CreateDynamicMapping(AttributeMapping mapping)
        {
            return new Domain.Entity.AssetAttributeDynamicMapping()
            {
                Id = mapping.GetId(),
                AssetId = mapping.GetAssetId(),
                AssetAttributeTemplateId = mapping.GetAssetAttributeTemplateId(),
                DeviceId = mapping.DeviceId,
                MetricKey = mapping.MetricKey,
                SequentialNumber = mapping.GetSequentialNumber()
            };
        }

        public static AttributeMapping CreateDto(Domain.Entity.AssetAttributeIntegrationMapping mapping)
        {
            return JObject.FromObject(new
            {
                AttributeType = TYPE_INTEGRATION,
                mapping.Id,
                mapping.AssetId,
                mapping.AssetAttributeTemplateId,
                mapping.IntegrationId,
                mapping.DeviceId,
                mapping.MetricKey,
                mapping.SequentialNumber
            }).ToObject<AttributeMapping>();
        }

        public static Domain.Entity.AssetAttributeIntegrationMapping CreateIntegrationMapping(AttributeMapping mapping)
        {
            return new Domain.Entity.AssetAttributeIntegrationMapping()
            {
                Id = mapping.GetId(),
                AssetId = mapping.GetAssetId(),
                AssetAttributeTemplateId = mapping.GetAssetAttributeTemplateId(),
                IntegrationId = mapping.GetNullableIdByKey("integrationId", "IntegrationId"),
                DeviceId = mapping.DeviceId,
                MetricKey = mapping.MetricKey,
                SequentialNumber = mapping.GetSequentialNumber()
            };
        }

        public static AttributeMapping CreateDto(Domain.Entity.AssetAttributeAliasMapping mapping)
        {
            return JObject.FromObject(new
            {
                AttributeType = TYPE_ALIAS,
                mapping.Id,
                mapping.AssetId,
                mapping.AssetAttributeTemplateId,
                mapping.AliasAssetId,
                mapping.AliasAttributeId,
                mapping.AliasAssetName,
                mapping.AliasAttributeName,
                mapping.DataType,
                mapping.UomId,
                mapping.DecimalPlace,
                mapping.ThousandSeparator
            }).ToObject<AttributeMapping>();
        }

        public static Domain.Entity.AssetAttributeAliasMapping CreateAliasMapping(AttributeMapping mapping)
        {
            return new Domain.Entity.AssetAttributeAliasMapping()
            {
                Id = mapping.GetId(),
                AssetId = mapping.GetAssetId(),
                AssetAttributeTemplateId = mapping.GetAssetAttributeTemplateId(),
                AliasAssetId = mapping.GetAliasAssetId(),
                AliasAttributeId = mapping.GetAliasAttributeId(),
                AliasAssetName = mapping.GetStringByKey("aliasAssetName", "AliasAssetName"),
                AliasAttributeName = mapping.GetStringByKey("aliasAttributeName", "AliasAttributeName"),
                DataType = mapping.DataType,
                UomId = mapping.GetUomId(),
                DecimalPlace = mapping.DecimalPlace,
                ThousandSeparator = mapping.ThousandSeparator
            };
        }

        public static AttributeMapping CreateDto(Domain.Entity.AssetAttributeCommandMapping mapping)
        {
            return JObject.FromObject(new
            {
                AttributeType = TYPE_COMMAND,
                mapping.Id,
                mapping.AssetId,
                mapping.AssetAttributeTemplateId,
                mapping.DeviceId,
                mapping.MetricKey,
                mapping.RowVersion,
                mapping.SequentialNumber
            }).ToObject<AttributeMapping>();
        }

        public static Domain.Entity.AssetAttributeCommandMapping CreateCommandMapping(AttributeMapping mapping)
        {
            return new Domain.Entity.AssetAttributeCommandMapping()
            {
                Id = mapping.GetId(),
                AssetId = mapping.GetAssetId(),
                AssetAttributeTemplateId = mapping.GetAssetAttributeTemplateId(),
                DeviceId = mapping.DeviceId,
                MetricKey = mapping.MetricKey,
                RowVersion = mapping.RowVersion.Value,
                SequentialNumber = mapping.GetSequentialNumber()
            };
        }

        public static AttributeMapping CreateDto(Domain.Entity.AssetAttributeRuntimeMapping mapping)
        {
            return JObject.FromObject(new
            {
                AttributeType = TYPE_RUNTIME,
                mapping.Id,
                mapping.AssetId,
                mapping.AssetAttributeTemplateId,
                mapping.EnabledExpression,
                mapping.Expression,
                mapping.ExpressionCompile,
                mapping.IsTriggerVisibility,
                mapping.SequentialNumber
            }).ToObject<AttributeMapping>();
        }

        public static Domain.Entity.AssetAttributeRuntimeMapping CreateRuntimeMapping(AttributeMapping mapping)
        {
            return new Domain.Entity.AssetAttributeRuntimeMapping()
            {
                Id = mapping.GetId(),
                AssetId = mapping.GetAssetId(),
                AssetAttributeTemplateId = mapping.GetAssetAttributeTemplateId(),
                EnabledExpression = mapping.EnabledExpression,
                Expression = mapping.GetExpression(),
                ExpressionCompile = mapping.GetExpressionCompile(),
                IsTriggerVisibility = mapping.GetBoolByKey("isTriggerVisibility", "IsTriggerVisibility"),
                SequentialNumber = mapping.GetSequentialNumber()
            };
        }

        public static AttributeMapping CreateDto(Domain.Entity.AssetAttributeRuntimeTrigger trigger)
        {
            return JObject.FromObject(new
            {
                trigger.Id,
                trigger.AssetId,
                trigger.AttributeId,
                trigger.TriggerAssetId,
                trigger.TriggerAttributeId,
                trigger.IsSelected
            }).ToObject<AttributeMapping>();
        }

        public static Domain.Entity.AssetAttributeRuntimeTrigger CreateRuntimeTrigger(AttributeMapping mapping)
        {
            return new Domain.Entity.AssetAttributeRuntimeTrigger()
            {
                Id = mapping.GetId(),
                AssetId = mapping.GetAssetId(),
                AttributeId = mapping.GetAttributeId(),
                TriggerAssetId = mapping.GetTriggerAssetId(),
                TriggerAttributeId = mapping.GetTriggerAttributeId(),
                IsSelected = mapping.GetBoolByKey("isSelected", "IsSelected")
            };
        }

        public static Domain.Entity.AssetAttribute CreateAttribute(this AssetAttributeDto dto)
        {
            return new Domain.Entity.AssetAttribute()
            {
                Id = dto.Id,
                AssetId = dto.AssetId,
                Name = dto.Name,
                Value = dto.Value?.ToString(),
                AttributeType = dto.AttributeType,
                DataType = dto.DataType,
                UomId = dto.UomId,
                DecimalPlace = dto.DecimalPlace,
                ThousandSeparator = dto.ThousandSeparator,
                SequentialNumber = dto.SequentialNumber,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow,
                AssetAttributeDynamic = CreateAttributeDynamic(dto),
                AssetAttributeIntegration = CreateAttributeIntegration(dto),
                AssetAttributeAlias = CreateAttributeAlias(dto),
                AssetAttributeCommand = CreateAttributeCommand(dto),
                AssetAttributeRuntime = CreateAttributeRuntime(dto)
            };
        }

        private static Domain.Entity.AssetAttributeDynamic CreateAttributeDynamic(AssetAttributeDto dto)
        {
            if (dto.AttributeType != TYPE_DYNAMIC)
                return null;

            return new Domain.Entity.AssetAttributeDynamic
            {
                Id = dto.Payload.GetId(),
                AssetAttributeId = dto.Id,
                DeviceId = dto.Payload.DeviceId,
                MetricKey = dto.Payload.MetricKey
            };
        }

        private static Domain.Entity.AssetAttributeIntegration CreateAttributeIntegration(AssetAttributeDto dto)
        {
            if (dto.AttributeType != TYPE_INTEGRATION)
                return null;

            return new Domain.Entity.AssetAttributeIntegration
            {
                Id = dto.Payload.GetId(),
                AssetAttributeId = dto.Id,
                DeviceId = dto.Payload.DeviceId,
                MetricKey = dto.Payload.MetricKey,
                IntegrationId = dto.Payload.GetIdByKey("integrationId", "IntegrationId")
            };
        }

        private static Domain.Entity.AssetAttributeAlias CreateAttributeAlias(AssetAttributeDto dto)
        {
            if (dto.AttributeType != TYPE_ALIAS)
                return null;

            var aliasAssetId = dto.Payload.GetAliasAssetId();
            var aliasAttributeId = dto.Payload.GetAliasAttributeId();
            return new Domain.Entity.AssetAttributeAlias
            {
                Id = dto.Payload.GetId(),
                AssetAttributeId = dto.Id,
                AliasAssetId = !IsNullOrEmptyGuid(aliasAssetId) ? aliasAssetId : null,
                AliasAttributeId = !IsNullOrEmptyGuid(aliasAttributeId) ? aliasAttributeId : null
            };
        }

        private static bool IsNullOrEmptyGuid(Guid? guid)
        {
            return guid == null || guid == Guid.Empty;
        }

        private static Domain.Entity.AssetAttributeCommand CreateAttributeCommand(AssetAttributeDto dto)
        {
            if (dto.AttributeType != TYPE_COMMAND)
                return null;

            return new Domain.Entity.AssetAttributeCommand
            {
                Id = dto.Payload.GetId(),
                AssetAttributeId = dto.Id,
                DeviceId = dto.Payload.DeviceId,
                MetricKey = dto.Payload.MetricKey,
                RowVersion = dto.Payload.RowVersion.Value,
                SequentialNumber = dto.Payload.GetSequentialNumber(),
            };
        }

        private static Domain.Entity.AssetAttributeRuntime CreateAttributeRuntime(AssetAttributeDto dto)
        {
            if (dto.AttributeType != TYPE_RUNTIME || dto.Payload == null)
                return null;

            return new Domain.Entity.AssetAttributeRuntime
            {
                Id = dto.Payload.GetId(),
                AssetAttributeId = dto.Id,
                EnabledExpression = dto.Payload?.EnabledExpression ?? false,
                Expression = dto.Payload.GetExpression(),
                ExpressionCompile = dto.Payload.GetExpressionCompile(),
                IsTriggerVisibility = dto.Payload.GetTriggerAttributeId() != Guid.Empty,
            };
        }

        public static Domain.Entity.AssetAttributeTemplate CreateAttributeTemplate(this GetAssetAttributeTemplateDto dto)
        {
            return new Domain.Entity.AssetAttributeTemplate()
            {
                Id = dto.Id,
                AssetTemplateId = dto.AssetTemplateId,
                Name = dto.Name,
                Value = dto.Value,
                AttributeType = dto.AttributeType,
                UomId = dto.UomId,
                DataType = dto.DataType,
                DecimalPlace = dto.DecimalPlace,
                ThousandSeparator = dto.ThousandSeparator,
                SequentialNumber = dto.SequentialNumber,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow,
                AssetAttributeDynamic = CreateAttributeTemplateDynamic(dto),
                AssetAttributeIntegration = CreateAttributeTemplateIntegration(dto),
                AssetAttributeCommand = CreateAttributeTemplateCommand(dto),
                AssetAttributeRuntime = CreateAttributeTemplateRuntime(dto)
            };
        }

        private static Domain.Entity.AssetAttributeDynamicTemplate CreateAttributeTemplateDynamic(GetAssetAttributeTemplateDto dto)
        {
            if (dto.AttributeType != TYPE_DYNAMIC || dto.Payload == null)
                return null;

            return new Domain.Entity.AssetAttributeDynamicTemplate
            {
                Id = dto.Payload.GetId(),
                AssetAttributeTemplateId = dto.Id,
                DeviceTemplateId = dto.Payload.GetDeviceTemplateId(),
                MarkupName = dto.Payload.GetMarkupName(),
                MetricKey = dto.Payload.MetricKey,
            };
        }

        private static Domain.Entity.AssetAttributeTemplateIntegration CreateAttributeTemplateIntegration(GetAssetAttributeTemplateDto dto)
        {
            if (dto.AttributeType != TYPE_INTEGRATION || dto.Payload == null)
                return null;

            return new Domain.Entity.AssetAttributeTemplateIntegration
            {
                Id = dto.Payload.GetId(),
                AssetAttributeTemplateId = dto.Id,
                IntegrationMarkupName = dto.Payload.GetIntegrationMarkupName(),
                IntegrationId = dto.Payload.GetIdByKey("integrationId", "IntegrationId"),
                DeviceMarkupName = dto.Payload.GetDeviceMarkupName(),
                DeviceId = dto.Payload.DeviceId,
                MetricKey = dto.Payload.MetricKey,
            };
        }

        private static Domain.Entity.AssetAttributeCommandTemplate CreateAttributeTemplateCommand(GetAssetAttributeTemplateDto dto)
        {
            if (dto.AttributeType != TYPE_COMMAND || dto.Payload == null)
                return null;

            return new Domain.Entity.AssetAttributeCommandTemplate
            {
                Id = dto.Payload.GetId(),
                AssetAttributeTemplateId = dto.Id,
                DeviceTemplateId = dto.Payload.GetDeviceTemplateId(),
                MarkupName = dto.Payload.GetMarkupName(),
                MetricKey = dto.Payload.MetricKey,
            };
        }

        private static Domain.Entity.AssetAttributeRuntimeTemplate CreateAttributeTemplateRuntime(GetAssetAttributeTemplateDto dto)
        {
            if (dto.AttributeType != TYPE_RUNTIME || dto.Payload == null)
                return null;

            return new Domain.Entity.AssetAttributeRuntimeTemplate
            {
                Id = dto.Payload.GetId(),
                AssetAttributeTemplateId = dto.Id,
                TriggerAttributeId = dto.Payload.GetTriggerAttributeId(),
                EnabledExpression = dto.Payload.EnabledExpression,
                Expression = dto.Payload.GetExpression(),
                ExpressionCompile = dto.Payload.GetExpressionCompile()
            };
        }
    }
}