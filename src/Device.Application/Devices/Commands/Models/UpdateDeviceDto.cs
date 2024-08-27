using System;
using System.Linq.Expressions;
using AHI.Infrastructure.Service.Tag.Model;
using AHI.Infrastructure.Service.Tag.Extension;

namespace Device.Application.Device.Command.Model
{
    public class UpdateDeviceDto : TagDtos
    {
        public string Id { get; set; }
        static Func<Domain.Entity.Device, UpdateDeviceDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.Device, UpdateDeviceDto>> Projection
        {
            get
            {
                return model => new UpdateDeviceDto
                {
                    Id = model.Id,
                    Tags = model.EntityTags.MappingTagDto()
                };
            }
        }

        public static UpdateDeviceDto Create(Domain.Entity.Device model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}