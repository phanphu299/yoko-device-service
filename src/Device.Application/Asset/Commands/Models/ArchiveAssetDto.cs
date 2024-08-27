using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Device.ApplicationExtension.Extension;
using static Device.Application.Constant.AttributeTypeConstants;

namespace Device.Application.Asset.Command.Model
{
    public class ArchiveAssetDto : GetAssetDto
    {
        public IEnumerable<AttributeMapping> Mappings;
        public IEnumerable<AttributeMapping> Triggers;

        static Func<Domain.Entity.Asset, ArchiveAssetDto> DtoConverter = DtoProjection.Compile();
        static Func<ArchiveAssetDto, Domain.Entity.Asset> EntityConverter = EntityProjection.Compile();

        private static Expression<Func<Domain.Entity.Asset, ArchiveAssetDto>> DtoProjection
        {
            get
            {
                return entity => new ArchiveAssetDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    ResourcePath = entity.ResourcePath,
                    ParentAssetId = entity.ParentAssetId,
                    AssetTemplateId = entity.AssetTemplateId,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    Attributes = GetAttribute(entity),
                    Mappings = GetMappingPayload(entity),
                    Triggers = entity.Triggers.Select(ArchiveExtension.CreateDto)
                };
            }
        }

        public static ArchiveAssetDto CreateDto(Domain.Entity.Asset entity)
        {
            if (entity != null)
            {
                return DtoConverter(entity);
            }
            return null;
        }

        private static Expression<Func<ArchiveAssetDto, Domain.Entity.Asset>> EntityProjection
        {
            get
            {
                return dto => new Domain.Entity.Asset
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    ResourcePath = dto.ResourcePath,
                    ParentAssetId = dto.ParentAssetId,
                    AssetTemplateId = dto.AssetTemplateId,
                    CreatedUtc = DateTime.UtcNow,
                    UpdatedUtc = DateTime.UtcNow,
                    Attributes = dto.Attributes.Select(ArchiveExtension.CreateAttribute).ToList(),
                    AssetAttributeDynamicMappings = CreateDynamicMapping(dto.Mappings).ToList(),
                    AssetAttributeStaticMappings = CreateStaticMapping(dto.Mappings).ToList(),
                    AssetAttributeRuntimeMappings = CreateRuntimeMapping(dto.Mappings).ToList(),
                    AssetAttributeIntegrationMappings = CreateIntegrationMapping(dto.Mappings).ToList(),
                    AssetAttributeCommandMappings = CreateCommandMapping(dto.Mappings).ToList(),
                    AssetAttributeAliasMappings = CreateAliasMapping(dto.Mappings).ToList(),
                    Triggers = dto.Triggers.Select(ArchiveExtension.CreateRuntimeTrigger).ToList()
                };
            }
        }

        public static Domain.Entity.Asset CreateEntity(ArchiveAssetDto dto, string archivedUpn)
        {
            if (dto != null)
            {
                var entity = EntityConverter(dto);
                entity.CreatedBy = archivedUpn;
                return entity;
            }
            return null;
        }

        private static IEnumerable<AssetAttributeDto> GetAttribute(Domain.Entity.Asset entity)
        {
            return entity.Attributes.Select(x =>
            {
                var attribute = AssetAttributeDto.Create(x);
                //az https://dev.azure.com/AssetHealthInsights/Asset%20Backlogs/_workitems/edit/75526
                if (string.IsNullOrEmpty(attribute.DataType) && attribute.AttributeType == TYPE_COMMAND && attribute.Payload != null && !string.IsNullOrEmpty(attribute.Payload.DataType))
                {
                    attribute.DataType = attribute.Payload.DataType;
                }
                return attribute;
            }).OrderBy(x => x.CreatedUtc).ThenBy(x => x.SequentialNumber);
        }

        private static IEnumerable<AttributeMapping> GetMappingPayload(Domain.Entity.Asset entity)
        {
            return entity.AssetAttributeStaticMappings.Select(ArchiveExtension.CreateDto)
            .Concat(entity.AssetAttributeDynamicMappings.Select(ArchiveExtension.CreateDto))
            .Concat(entity.AssetAttributeIntegrationMappings.Select(ArchiveExtension.CreateDto))
            .Concat(entity.AssetAttributeCommandMappings.Select(ArchiveExtension.CreateDto))
            .Concat(entity.AssetAttributeAliasMappings.Select(ArchiveExtension.CreateDto))
            .Concat(entity.AssetAttributeRuntimeMappings.Select(ArchiveExtension.CreateDto));
        }

        private static IEnumerable<Domain.Entity.AssetAttributeStaticMapping> CreateStaticMapping(IEnumerable<AttributeMapping> mappings)
        {
            return mappings.Where(mapping => mapping.GetAttributeType() == TYPE_STATIC).Select(ArchiveExtension.CreateStaticMapping);
        }

        private static IEnumerable<Domain.Entity.AssetAttributeDynamicMapping> CreateDynamicMapping(IEnumerable<AttributeMapping> mappings)
        {
            return mappings.Where(mapping => mapping.GetAttributeType() == TYPE_DYNAMIC).Select(ArchiveExtension.CreateDynamicMapping);
        }

        private static IEnumerable<Domain.Entity.AssetAttributeIntegrationMapping> CreateIntegrationMapping(IEnumerable<AttributeMapping> mappings)
        {
            return mappings.Where(mapping => mapping.GetAttributeType() == TYPE_INTEGRATION).Select(ArchiveExtension.CreateIntegrationMapping);
        }

        private static IEnumerable<Domain.Entity.AssetAttributeAliasMapping> CreateAliasMapping(IEnumerable<AttributeMapping> mappings)
        {
            return mappings.Where(mapping => mapping.GetAttributeType() == TYPE_ALIAS).Select(ArchiveExtension.CreateAliasMapping);
        }

        private static IEnumerable<Domain.Entity.AssetAttributeCommandMapping> CreateCommandMapping(IEnumerable<AttributeMapping> mappings)
        {
            return mappings.Where(mapping => mapping.GetAttributeType() == TYPE_COMMAND).Select(ArchiveExtension.CreateCommandMapping);
        }

        private static IEnumerable<Domain.Entity.AssetAttributeRuntimeMapping> CreateRuntimeMapping(IEnumerable<AttributeMapping> mappings)
        {
            return mappings.Where(mapping => mapping.GetAttributeType() == TYPE_RUNTIME).Select(ArchiveExtension.CreateRuntimeMapping);
        }
    }
}
