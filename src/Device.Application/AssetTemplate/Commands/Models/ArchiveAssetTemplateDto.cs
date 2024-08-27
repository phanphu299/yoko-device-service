using System;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.AssetAttributeTemplate.Command.Model;
using Device.ApplicationExtension.Extension;

namespace Device.Application.AssetTemplate.Command.Model
{
    public class ArchiveAssetTemplateDto : GetAssetTemplateDto
    {
        public string ResourcePath { get; set; }

        static Func<Domain.Entity.AssetTemplate, ArchiveAssetTemplateDto> DtoConverter = DtoProjection.Compile();
        static Func<ArchiveAssetTemplateDto, Domain.Entity.AssetTemplate> EntityConverter = EntityProjection.Compile();

        private static Expression<Func<Domain.Entity.AssetTemplate, ArchiveAssetTemplateDto>> DtoProjection
        {
            get
            {
                return entity => new ArchiveAssetTemplateDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    ResourcePath = entity.ResourcePath,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    Attributes = entity.Attributes.Select(GetAssetAttributeTemplateDto.Create).OrderBy(x => x.CreatedUtc).ThenBy(x => x.SequentialNumber),
                };
            }
        }

        public static ArchiveAssetTemplateDto CreateDto(Domain.Entity.AssetTemplate entity)
        {
            if (entity != null)
            {
                return DtoConverter(entity);
            }
            return null;
        }

        private static Expression<Func<ArchiveAssetTemplateDto, Domain.Entity.AssetTemplate>> EntityProjection
        {
            get
            {
                return dto => new Domain.Entity.AssetTemplate
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    ResourcePath = dto.ResourcePath,
                    CreatedUtc = DateTime.UtcNow,
                    UpdatedUtc = DateTime.UtcNow,
                    Attributes = dto.Attributes.Select(ArchiveExtension.CreateAttributeTemplate).ToList(),
                };
            }
        }

        public static Domain.Entity.AssetTemplate CreateEntity(ArchiveAssetTemplateDto dto, string archivedUpn)
        {
            if (dto != null)
            {
                var entity = EntityConverter(dto);
                entity.CreatedBy = archivedUpn;
                return entity;
            }
            return null;
        }
    }
}