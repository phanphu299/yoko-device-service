using System;
using System.Linq.Expressions;

namespace Device.Application.Template.Command.Model
{
    public class GetTemplateSimpleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool Deleted { set; get; }
        public int TotalMetric { set; get; }
        public string CreatedBy { get; set; }
        static Func<Domain.Entity.DeviceTemplate, GetTemplateSimpleDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.DeviceTemplate, GetTemplateSimpleDto>> Projection
        {
            get
            {
                return entity => new GetTemplateSimpleDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Deleted = entity.Deleted,
                    TotalMetric = entity.TotalMetric,
                    CreatedBy = entity.CreatedBy
                };
            }
        }
        public static GetTemplateSimpleDto Create(Domain.Entity.DeviceTemplate model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
