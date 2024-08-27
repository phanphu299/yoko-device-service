using System;
using System.Linq.Expressions;
using AHI.Infrastructure.Service.Tag.Model;
using AHI.Infrastructure.Service.Tag.Extension;

namespace Device.Application.AssetTemplate.Command.Model
{
    public class AddAssetTemplateDto : TagDtos
    {
        public Guid Id { get; set; }
        static Func<Domain.Entity.AssetTemplate, AddAssetTemplateDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.AssetTemplate, AddAssetTemplateDto>> Projection
        {
            get
            {
                return model => new AddAssetTemplateDto
                {
                    Id = model.Id,
                    Tags = model.EntityTags.MappingTagDto()
                };
            }
        }

        public static AddAssetTemplateDto Create(Domain.Entity.AssetTemplate model)
        {
            return Converter(model);
        }
    }
}