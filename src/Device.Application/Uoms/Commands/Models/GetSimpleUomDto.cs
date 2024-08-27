using System;
using System.Linq.Expressions;
using MessagePack;

namespace Device.Application.Uom.Command.Model
{
    [MessagePackObject]
    public class GetSimpleUomDto
    {
        [Key("id")]
        public int? Id { get; set; }
        [Key("name")]
        public string Name { get; set; }
        [Key("abbreviation")]
        public string Abbreviation { get; set; }

        public GetSimpleUomDto()
        {
        }

        public GetSimpleUomDto(int? id, string name, string abbreviation)
        {
            Id = id;
            Name = name;
            Abbreviation = abbreviation;
        }
        static Func<Domain.Entity.Uom, GetSimpleUomDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.Uom, GetSimpleUomDto>> Projection
        {
            get
            {
                return entity => new GetSimpleUomDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Abbreviation = entity.Abbreviation
                };
            }
        }

        public static GetSimpleUomDto Create(Domain.Entity.Uom model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }

    }

}
