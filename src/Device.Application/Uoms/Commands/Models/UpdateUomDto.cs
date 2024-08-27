using System;
using System.Linq.Expressions;

namespace Device.Application.Uom.Command.Model
{
    public class UpdateUomsDto
    {
        public int Id { get; set; }
        static Func<Domain.Entity.Uom, UpdateUomsDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.Uom, UpdateUomsDto>> Projection
        {
            get
            {
                return model => new UpdateUomsDto
                {
                    Id = model.Id
                };
            }
        }

        public static UpdateUomsDto Create(Domain.Entity.Uom model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
