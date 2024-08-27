using System;
using System.Linq.Expressions;

namespace Device.Application.TemplateKeyType.Command.Model
{
    public class GetTemplateKeyTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Deleted { get; set; }
        static Func<Domain.Entity.TemplateKeyType, GetTemplateKeyTypeDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.TemplateKeyType, GetTemplateKeyTypeDto>> Projection
        {
            get
            {
                return entity => new GetTemplateKeyTypeDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Deleted = entity.Deleted
                };
            }
        }

        public static GetTemplateKeyTypeDto Create(Domain.Entity.TemplateKeyType model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
