using System;
using System.Linq.Expressions;

namespace Device.Application.BlockCategory.Model
{
    public class BlockCategoryHierarchyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? ParentCategoryId { get; set; }
        public bool IsCategory { get; set; }
        public string Type { get; set; }

        static Func<Domain.Entity.CategoryHierarchy, BlockCategoryHierarchyDto> Converter = Projection.Compile();

        private static Expression<Func<Domain.Entity.CategoryHierarchy, BlockCategoryHierarchyDto>> Projection
        {
            get
            {
                return model => new BlockCategoryHierarchyDto
                {
                    Id = model.Id,
                    Name = model.Name,
                    IsCategory = model.IsCategory,
                    Type = model.Type,
                    ParentCategoryId = model.ParentCategoryId
                };
            }
        }
        public static BlockCategoryHierarchyDto Create(Domain.Entity.CategoryHierarchy entity)
        {
            if (entity == null)
            {
                return null;
            }

            return Converter(entity);
        }
    }
}
