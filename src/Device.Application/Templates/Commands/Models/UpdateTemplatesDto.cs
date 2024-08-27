using System;
using System.Linq.Expressions;
using AHI.Infrastructure.Service.Tag.Extension;
using AHI.Infrastructure.Service.Tag.Model;

namespace Device.Application.Template.Command.Model
{
    public class UpdateTemplatesDto : TagDtos
    {
        public Guid Id { get; set; }
        static Func<Domain.Entity.DeviceTemplate, UpdateTemplatesDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.DeviceTemplate, UpdateTemplatesDto>> Projection
        {
            get
            {
                return model => new UpdateTemplatesDto
                {
                    Id = model.Id,
                    Tags = model.EntityTags.MappingTagDto()
                };
            }
        }

        public static UpdateTemplatesDto Create(Domain.Entity.DeviceTemplate model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}