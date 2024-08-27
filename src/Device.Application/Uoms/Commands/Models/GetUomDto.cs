using System;
using System.Linq.Expressions;
using AHI.Infrastructure.Service.Tag.Extension;
using AHI.Infrastructure.Service.Tag.Model;
using Device.ApplicationExtension.Extension;

namespace Device.Application.Uom.Command.Model
{
    public class GetUomDto : TagDtos
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Abbreviation { get; set; }
        public string LookupCode { get; set; }
        public string RefFactor { get; set; }
        public string RefOffset { get; set; }
        public string CanonicalFactor { get; set; }
        public string CanonicalOffset { get; set; }
        public int? RefId { get; set; }
        public bool System { get; set; }
        public GetUomDto RefUom { get; set; }
        public DateTime? CreatedUtc { get; set; }
        public DateTime? UpdatedUtc { get; set; }
        public string CreatedBy { get; set; }
        public string ResourcePath { get; set; }
        static Func<Domain.Entity.Uom, GetUomDto> Converter = Projection.Compile();

        private static Expression<Func<Domain.Entity.Uom, GetUomDto>> Projection
        {
            get
            {
                return entity => new GetUomDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    RefFactor = StringExtension.ToLongString(entity.RefFactor ?? 1),
                    RefOffset = StringExtension.ToLongString(entity.RefOffset ?? 0),
                    LookupCode = entity.LookupCode,
                    RefId = entity.RefId,
                    System = entity.System,
                    CanonicalFactor = StringExtension.ToLongString(entity.CanonicalFactor ?? 1),
                    CanonicalOffset = StringExtension.ToLongString(entity.CanonicalOffset ?? 0),
                    Abbreviation = entity.Abbreviation,
                    Description = entity.Description,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    CreatedBy = entity.CreatedBy,
                    ResourcePath = entity.ResourcePath,
                    RefUom = GetUomDto.Create(entity.RefUom),
                    Tags = entity.EntityTags.MappingTagDto()
                };
            }
        }

        public static GetUomDto Create(Domain.Entity.Uom model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
