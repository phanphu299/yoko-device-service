using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.TemplateDetail.Command.Model;

namespace Device.Application.TemplatePayload.Command.Model
{
    public class GetTemplatePayloadDto
    {
        public int Id { get; set; }
        public Guid TemplateId { get; set; }
        public string JsonPayload { get; set; }
        public IEnumerable<GetTemplateDetailsDto> Details { get; set; } = new List<GetTemplateDetailsDto>();
        static Func<Domain.Entity.TemplatePayload, GetTemplatePayloadDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.TemplatePayload, GetTemplatePayloadDto>> Projection
        {
            get
            {
                return entity => new GetTemplatePayloadDto
                {
                    Id = entity.Id,
                    TemplateId = entity.TemplateId,
                    JsonPayload = entity.JsonPayload,
                    Details = entity.Details.OrderBy(detail => detail.Id).Select(GetTemplateDetailsDto.Create),

                };
            }
        }
        public static GetTemplatePayloadDto Create(Domain.Entity.TemplatePayload entity)
        {
            if (entity != null)
            {
                return Converter(entity);
            }
            return null;
        }
    }
}
