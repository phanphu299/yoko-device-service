using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.BlockBinding.Command.Model;

namespace Device.Application.Block.Command.Model
{
    public class GetFunctionBlockSimpleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public Guid? CategoryId { get; set; }
        public bool System { get; set; }
        public IEnumerable<GetFunctionBlockBindingDto> Bindings { get; set; } = new List<GetFunctionBlockBindingDto>();
        private static Func<Domain.Entity.FunctionBlock, GetFunctionBlockSimpleDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.FunctionBlock, GetFunctionBlockSimpleDto>> Projection
        {
            get
            {
                return entity => new GetFunctionBlockSimpleDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Type = entity.Type,
                    // IsActive = entity.IsActive,
                    CategoryId = entity.CategoryId,
                    System = entity.System,
                    Bindings = entity.Bindings.OrderBy(x => x.BindingType).ThenBy(x => x.SequentialNumber).Select(GetFunctionBlockBindingDto.Create)
                };
            }
        }

        public static GetFunctionBlockSimpleDto Create(Domain.Entity.FunctionBlock entity)
        {
            if (entity != null)
            {
                return Converter(entity);
            }
            return null;
        }
    }
}
