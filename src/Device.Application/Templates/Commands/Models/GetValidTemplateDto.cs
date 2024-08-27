using System;
using System.Linq.Expressions;
using Device.Domain.Entity;

namespace Device.Application.Template.Command.Model
{
    public class GetValidTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public bool Deleted { get; set; }
        static Func<ValidTemplate, GetValidTemplateDto> Converter = Projection.Compile();
        private static Expression<Func<ValidTemplate, GetValidTemplateDto>> Projection
        {
            get
            {
                return entity => new GetValidTemplateDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Deleted = entity.Deleted,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc
                };
            }
        }

        public static GetValidTemplateDto Create(Domain.Entity.ValidTemplate model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
