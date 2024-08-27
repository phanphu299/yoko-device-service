using System;
using System.Linq.Expressions;

namespace Device.Application.Uom.Command.Model
{
    public class ArchiveUomDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Abbreviation { get; set; }
        public string LookupCode { get; set; }
        public double RefFactor { get; set; }
        public double RefOffset { get; set; }
        public double CanonicalFactor { get; set; }
        public double CanonicalOffset { get; set; }
        public int? RefId { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        static Func<Domain.Entity.Uom, ArchiveUomDto> DtoConverter = DtoProjection.Compile();

        private static Expression<Func<Domain.Entity.Uom, ArchiveUomDto>> DtoProjection
        {
            get
            {
                return entity => new ArchiveUomDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    RefFactor = (double)entity.RefFactor,
                    RefOffset = (double)entity.RefOffset,
                    LookupCode = entity.LookupCode,
                    RefId = entity.RefId,
                    CanonicalFactor = (double)entity.CanonicalFactor,
                    CanonicalOffset = (double)entity.CanonicalOffset,
                    Abbreviation = entity.Abbreviation,
                    Description = entity.Description,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc
                };
            }
        }

        public static ArchiveUomDto Create(Domain.Entity.Uom model)
        {
            if (model != null)
            {
                return DtoConverter(model);
            }
            return null;
        }

        static Func<ArchiveUomDto, Domain.Entity.Uom> EntityConverter = EntityProjection.Compile();

        private static Expression<Func<ArchiveUomDto, Domain.Entity.Uom>> EntityProjection
        {
            get
            {
                return x => new Domain.Entity.Uom
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    Abbreviation = x.Abbreviation,
                    LookupCode = x.LookupCode,
                    RefFactor = x.RefFactor,
                    RefOffset = x.RefOffset,
                    CanonicalFactor = x.CanonicalFactor,
                    CanonicalOffset = x.CanonicalOffset,
                    RefId = x.RefId,
                    CreatedUtc = DateTime.UtcNow,
                    UpdatedUtc = DateTime.UtcNow
                };
            }
        }

        public static Domain.Entity.Uom CreateEntity(ArchiveUomDto model)
        {
            if (model != null)
            {
                return EntityConverter(model);
            }
            return null;
        }
    }
}
