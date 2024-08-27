using System;
using System.Linq.Expressions;
using AHI.Infrastructure.Service.Tag.Model;
using AHI.Infrastructure.Service.Tag.Extension;

namespace Device.Application.AssetTemplate.Command.Model
{
    public class UpdateAssetTemplateDto : TagDtos
    {
        public Guid Id { get; set; }
        static Func<Domain.Entity.AssetTemplate, UpdateAssetTemplateDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.AssetTemplate, UpdateAssetTemplateDto>> Projection
        {
            get
            {
                return model => new UpdateAssetTemplateDto
                {
                    Id = model.Id,
                    Tags = model.EntityTags.MappingTagDto()

                };
            }
        }

        public static UpdateAssetTemplateDto Create(Domain.Entity.AssetTemplate model)
        {
            return Converter(model);
        }
    }
}