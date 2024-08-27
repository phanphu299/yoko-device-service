using System;
using System.Linq.Expressions;
using Device.ApplicationExtension.Extension;

namespace Device.Application.TemplateBinding.Command.Model
{
    public class GetTemplateBindingDto
    {
        public int Id { get; set; }
        public Guid TemplateId { get; set; }
        public string Key { get; set; }
        public string DataType { get; set; }
        public string DefaultValue { get; set; }
        static Func<Domain.Entity.TemplateBinding, GetTemplateBindingDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.TemplateBinding, GetTemplateBindingDto>> Projection
        {
            get
            {
                return entity => new GetTemplateBindingDto()
                {
                    Id = entity.Id,
                    TemplateId = entity.TemplateId,
                    Key = entity.Key,
                    DataType = entity.DataType,
                    DefaultValue = entity.DefaultValue.FormatValueByDataType(entity.DataType)
                };
            }
        }
        public static GetTemplateBindingDto Create(Domain.Entity.TemplateBinding model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
    public class TemplateBindingSimpleDto
    {
        public string Key { get; set; }
        public string DataTypeName { get; set; }
        public string DefaultValue { get; set; }
        static Func<Domain.Entity.TemplateBinding, TemplateBindingSimpleDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.TemplateBinding, TemplateBindingSimpleDto>> Projection
        {
            get
            {
                return entity => new TemplateBindingSimpleDto()
                {
                    Key = entity.Key,
                    DataTypeName = entity.DataType,
                    DefaultValue = entity.DefaultValue
                };
            }
        }
        public static TemplateBindingSimpleDto Create(Domain.Entity.TemplateBinding model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
