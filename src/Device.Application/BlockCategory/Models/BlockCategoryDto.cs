using System;
using System.Linq.Expressions;

namespace Device.Application.BlockFunctionCategory.Model
{
    public class BlockCategoryDto
    {
        public Guid Id { get; set; }
        static Func<Domain.Entity.FunctionBlockCategory, BlockCategoryDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.FunctionBlockCategory, BlockCategoryDto>> Projection
        {
            get
            {
                return model => new BlockCategoryDto
                {
                    Id = model.Id,
                };
            }
        }

        public static BlockCategoryDto Create(Domain.Entity.FunctionBlockCategory entity)
        {
            return Converter(entity);
        }
    }
}
