using System.Collections.Generic;
using System;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.Block.Command.Model;

namespace Device.Application.BlockFunctionCategory.Model
{
    public class GetBlockCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public bool System { get; set; }

        public GetBlockCategoryDto Parent { get; set; }
        public IEnumerable<GetFunctionBlockDto> FunctionBlocks { get; set; } = new List<GetFunctionBlockDto>();
        public IEnumerable<ChildBlockCategoryDto> Children { get; set; } = new List<ChildBlockCategoryDto>();

        static Func<Domain.Entity.FunctionBlockCategory, GetBlockCategoryDto> Converter = Projection.Compile();

        private static Expression<Func<Domain.Entity.FunctionBlockCategory, GetBlockCategoryDto>> Projection
        {
            get
            {
                return model => new GetBlockCategoryDto
                {
                    Id = model.Id,
                    Name = model.Name,
                    CreatedUtc = model.CreatedUtc,
                    UpdatedUtc = model.UpdatedUtc,
                    System = model.System,
                    ParentId = model.ParentId,
                    Parent = Create(model.Parent),
                    Children = model.Children.OrderBy(x => x.CreatedUtc).Select(ChildBlockCategoryDto.Create),
                    FunctionBlocks = model.FunctionBlocks.OrderBy(x => x.CreatedUtc).Select(GetFunctionBlockDto.Create)
                };
            }
        }

        public static GetBlockCategoryDto Create(Domain.Entity.FunctionBlockCategory entity)
        {
            if (entity != null)
            {
                return Converter(entity);
            }
            return null;
        }
    }
}
