using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Device.Application.BlockCategory.Model
{
    public class GetBlockCategoryHierarchyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsCategory { get; set; }
        public string Type { get; set; }
        public IEnumerable<BlockCategoryHierarchyDto> Hierarchy { get; set; }

        static Func<Domain.Entity.FunctionBlockCategoryHierarchy, GetBlockCategoryHierarchyDto> Converter = Projection.Compile();

        private static Expression<Func<Domain.Entity.FunctionBlockCategoryHierarchy, GetBlockCategoryHierarchyDto>> Projection
        {
            get
            {
                return model => new GetBlockCategoryHierarchyDto
                {
                    Id = model.EntityId,
                    Name = model.EntityName,
                    IsCategory = model.EntityIsCategory,
                    Type = model.EntityType,
                    Hierarchy = model.Hierarchy.Select(BlockCategoryHierarchyDto.Create)
                };
            }
        }

        public static GetBlockCategoryHierarchyDto Create(Domain.Entity.FunctionBlockCategoryHierarchy entity)
        {
            if (entity == null)
            {
                return null;
            }

            return Converter(entity);
        }
    }
}
