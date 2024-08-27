using System;
using System.Linq.Expressions;
using AHI.Infrastructure.Service.Tag.Model;
using AHI.Infrastructure.Service.Tag.Extension;

namespace Device.Application.Device.Command.Model
{
    public class AddDeviceDto: TagDtos
    {
        public string Id { get; set; }
        static Func<Domain.Entity.Device, AddDeviceDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.Device, AddDeviceDto>> Projection
        {
            get
            {
                return model => new AddDeviceDto
                {
                    Id = model.Id,
                    Tags = model.EntityTags.MappingTagDto()
                };
            }
        }

        public static AddDeviceDto Create(Domain.Entity.Device model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
