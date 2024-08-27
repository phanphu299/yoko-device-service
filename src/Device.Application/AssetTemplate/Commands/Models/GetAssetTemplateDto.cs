using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AHI.Infrastructure.Service.Tag.Model;
using Device.Application.AssetAttributeTemplate.Command.Model;
using AHI.Infrastructure.Service.Tag.Extension;
using Device.ApplicationExtension.Extension;

namespace Device.Application.AssetTemplate.Command.Model
{
    public class GetAssetTemplateDto : TagDtos
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string NormalizeName => Name.NormalizeAHIName();
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public int AssetCount { get; set; }
        public IEnumerable<GetAssetAttributeTemplateDto> Attributes { get; set; } = new List<GetAssetAttributeTemplateDto>();
        public string LockedByUpn { get; set; }
        public string CreatedBy { get; set; }
        static Func<Domain.Entity.AssetTemplate, GetAssetTemplateDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.AssetTemplate, GetAssetTemplateDto>> Projection
        {
            get
            {
                return entity => new GetAssetTemplateDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    Attributes = entity.Attributes.OrderBy(x => x.CreatedUtc).ThenBy(x => x.SequentialNumber).Select(GetAssetAttributeTemplateDto.Create).ToList(),
                    AssetCount = entity.Assets.Count(),
                    CreatedBy = entity.CreatedBy,
                    Tags = entity.EntityTags.MappingTagDto()
                };
            }
        }

        public static GetAssetTemplateDto Create(Domain.Entity.AssetTemplate entity)
        {
            if (entity != null)
                return Converter(entity);
            return null;
        }
    }
}
