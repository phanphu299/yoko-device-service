using System;
using System.Linq.Expressions;

namespace Device.Application.BlockFunctionCategory.Model
{
    public class ArchiveBlockCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? ParentId { get; set; }
        public bool Deleted { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }

        static Func<Domain.Entity.FunctionBlockCategory, ArchiveBlockCategoryDto> DtoConverter = DtoProjection.Compile();
        static Func<ArchiveBlockCategoryDto, Domain.Entity.FunctionBlockCategory> EntityConverter = EntityProjection.Compile();

        private static Expression<Func<Domain.Entity.FunctionBlockCategory, ArchiveBlockCategoryDto>> DtoProjection
        {
            get
            {
                return entity => new ArchiveBlockCategoryDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    ParentId = entity.ParentId,
                    Deleted = entity.Deleted,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc
                };
            }
        }

        private static Expression<Func<ArchiveBlockCategoryDto, Domain.Entity.FunctionBlockCategory>> EntityProjection
        {
            get
            {
                return dto => new Domain.Entity.FunctionBlockCategory
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    ParentId = dto.ParentId,
                    Deleted = dto.Deleted,
                    CreatedUtc = DateTime.UtcNow,
                    UpdatedUtc = DateTime.UtcNow
                };
            }
        }

        public static ArchiveBlockCategoryDto Create(Domain.Entity.FunctionBlockCategory entity)
        {
            if (entity != null)
            {
                return DtoConverter(entity);
            }
            return null;
        }

        public static Domain.Entity.FunctionBlockCategory Create(ArchiveBlockCategoryDto dto)
        {
            if (dto != null)
            {
                return EntityConverter(dto);
            }
            return null;
        }
    }
}
