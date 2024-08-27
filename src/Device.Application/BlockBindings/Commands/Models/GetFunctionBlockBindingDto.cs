using System;
using System.Linq.Expressions;
using Device.ApplicationExtension.Extension;

namespace Device.Application.BlockBinding.Command.Model
{
    public class GetFunctionBlockBindingDto
    {
        public Guid Id { get; set; }
        public Guid FunctionBlockId { get; set; }
        public string Key { get; set; }
        public string DataType { get; set; }
        public string DefaultValue { get; set; }
        public string Description { get; set; }
        public bool Deleted { get; set; }
        public string BindingType { get; set; }
        public bool System { get; set; }
        static Func<Domain.Entity.FunctionBlockBinding, GetFunctionBlockBindingDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.FunctionBlockBinding, GetFunctionBlockBindingDto>> Projection
        {
            get
            {
                return entity => new GetFunctionBlockBindingDto()
                {
                    Id = entity.Id,
                    FunctionBlockId = entity.FunctionBlockId,
                    Key = entity.Key,
                    DataType = entity.DataType,
                    DefaultValue = entity.DefaultValue.FormatValueByDataType(entity.DataType),
                    Description = entity.Description,
                    Deleted = entity.Deleted,
                    BindingType = entity.BindingType,
                    System = entity.System
                };
            }
        }
        public static GetFunctionBlockBindingDto Create(Domain.Entity.FunctionBlockBinding entity)
        {
            if (entity == null)
                return null;
            return Converter.Invoke(entity);
        }
    }
}
