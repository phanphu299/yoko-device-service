using System;
using System.Linq.Expressions;

namespace Device.Application.Uom.Command.Model
{
    public class AddUomsDto
    {
        public int Id { get; set; }
        static Func<Domain.Entity.Uom, AddUomsDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.Uom, AddUomsDto>> Projection
        {
            get
            {
                return model => new AddUomsDto
                {
                    Id = model.Id
                };
            }
        }

        public static AddUomsDto Create(Domain.Entity.Uom model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
