using System;
using System.Linq.Expressions;
using Device.ApplicationExtension.Extension;

namespace Device.Application.Template.Command.Model
{
    public class ArchiveTemplateBindingDto
    {
        public int Id { get; set; }
        public Guid TemplateId { get; set; }
        public string Key { get; set; }
        public string DataType { get; set; }
        public string DefaultValue { get; set; }
        static Func<Domain.Entity.TemplateBinding, ArchiveTemplateBindingDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.TemplateBinding, ArchiveTemplateBindingDto>> Projection
        {
            get
            {
                return entity => new ArchiveTemplateBindingDto()
                {
                    Id = entity.Id,
                    TemplateId = entity.TemplateId,
                    Key = entity.Key,
                    DataType = entity.DataType,
                    DefaultValue = entity.DefaultValue.FormatValueByDataType(entity.DataType)
                };
            }
        }
        public static ArchiveTemplateBindingDto Create(Domain.Entity.TemplateBinding model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
