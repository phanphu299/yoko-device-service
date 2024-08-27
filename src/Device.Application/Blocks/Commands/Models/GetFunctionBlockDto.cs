using System;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.BlockBinding.Command.Model;

namespace Device.Application.Block.Command.Model
{
    public class GetFunctionBlockDto : GetFunctionBlockSimpleDto
    {
        // public bool IsActive { get; set; } = true;
        public bool Deleted { set; get; }
        public string BlockContent { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public string CreatedBy { get; set; }
        private static Func<Domain.Entity.FunctionBlock, GetFunctionBlockDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.FunctionBlock, GetFunctionBlockDto>> Projection
        {
            get
            {
                return entity => new GetFunctionBlockDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    BlockContent = entity.BlockContent,
                    Type = entity.Type,
                    // IsActive = entity.IsActive,
                    Deleted = entity.Deleted,
                    CategoryId = entity.CategoryId,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    CreatedBy = entity.CreatedBy,
                    System = entity.System,
                    Bindings = entity.Bindings.OrderBy(x => x.BindingType).ThenBy(x => x.SequentialNumber).Select(GetFunctionBlockBindingDto.Create)
                };
            }
        }

        public static new GetFunctionBlockDto Create(Domain.Entity.FunctionBlock entity)
        {
            if (entity != null)
            {
                return Converter(entity);
            }
            return null;
        }
    }
}
