
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.TemplateDetail.Command;
using MediatR;

namespace Device.Application.TemplatePayload.Command
{
    public class UpdateTemplatePayload : IRequest<BaseResponse>
    {
        public int Id { get; set; }
        public Guid TemplateId { get; set; }
        public string JsonPayload { get; set; }
        public ICollection<UpdateTemplateDetails> Details { get; set; } = new List<UpdateTemplateDetails>();
        static Func<UpdateTemplatePayload, Domain.Entity.TemplatePayload> Converter = Projection.Compile();
        private static Expression<Func<UpdateTemplatePayload, Domain.Entity.TemplatePayload>> Projection
        {
            get
            {
                return entity => new Domain.Entity.TemplatePayload
                {
                    Id = entity.Id,
                    TemplateId = entity.TemplateId,
                    JsonPayload = entity.JsonPayload,
                    Details = entity.Details.Select(e => UpdateTemplateDetails.Create(e)).ToList()
                };
            }
        }

        public static Domain.Entity.TemplatePayload Create(UpdateTemplatePayload model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
