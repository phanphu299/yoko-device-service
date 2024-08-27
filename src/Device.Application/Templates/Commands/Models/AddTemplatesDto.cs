using System;
using System.Linq.Expressions;
using AHI.Infrastructure.Service.Tag.Model;
using AHI.Infrastructure.Service.Tag.Extension;

namespace Device.Application.Template.Command.Model
{
    public class AddTemplatesDto : TagDtos
    {
        public Guid Id { get; set; }
        static Func<Domain.Entity.DeviceTemplate, AddTemplatesDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.DeviceTemplate, AddTemplatesDto>> Projection
        {
            get
            {
                return model => new AddTemplatesDto
                {
                    Id = model.Id,
                    Tags = model.EntityTags.MappingTagDto()
                };
            }
        }

        public static AddTemplatesDto Create(Domain.Entity.DeviceTemplate model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}