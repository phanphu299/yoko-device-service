using System;
using System.Linq.Expressions;

namespace Device.Application.BlockFunctionCategory.Model
{
    public class ChildBlockCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }

        static Func<Domain.Entity.FunctionBlockCategory, ChildBlockCategoryDto> Converter = Projection.Compile();

        private static Expression<Func<Domain.Entity.FunctionBlockCategory, ChildBlockCategoryDto>> Projection
        {
            get
            {
                return model => new ChildBlockCategoryDto
                {
                    Id = model.Id,
                    Name = model.Name,
                    CreatedUtc = model.CreatedUtc,
                    UpdatedUtc = model.UpdatedUtc,
                };
            }
        }

        public static ChildBlockCategoryDto Create(Domain.Entity.FunctionBlockCategory entity)
        {
            if (entity != null)
            {
                return Converter(entity);
            }
            return null;
        }
    }
}
