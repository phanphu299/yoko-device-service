using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Device.Application.Template.Command.Model
{
    public class ArchiveTemplatePayloadDto
    {
        public int Id { get; set; }
        public Guid? TemplateId { get; set; }
        public string JsonPayload { get; set; }
        public IEnumerable<ArchiveTemplateDetailDto> Details { get; set; } = new List<ArchiveTemplateDetailDto>();
        static Func<Domain.Entity.TemplatePayload, ArchiveTemplatePayloadDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.TemplatePayload, ArchiveTemplatePayloadDto>> Projection
        {
            get
            {
                return entity => new ArchiveTemplatePayloadDto
                {
                    Id = entity.Id,
                    TemplateId = entity.TemplateId,
                    JsonPayload = entity.JsonPayload,
                    Details = entity.Details.Select(ArchiveTemplateDetailDto.Create),

                };
            }
        }
        public static ArchiveTemplatePayloadDto Create(Domain.Entity.TemplatePayload entity)
        {
            if (entity != null)
            {
                return Converter(entity);
            }
            return null;
        }
    }
}
