using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.Constants;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.TemplateDetail.Command;
using MediatR;
using AHI.Infrastructure.Validation.CustomAttribute;

namespace Device.Application.TemplatePayload.Command
{
    public class AddTemplatePayload : IRequest<BaseResponse>, INotification
    {
        public Guid TemplateId { get; set; }

        [DynamicValidation(RemoteValidationKeys.payload)]
        public string JsonPayload { get; set; }

        public ICollection<AddTemplateDetails> Details { get; set; } = new List<AddTemplateDetails>();
        static Func<AddTemplatePayload, Domain.Entity.TemplatePayload> Converter = Projection.Compile();
        private static Expression<Func<AddTemplatePayload, Domain.Entity.TemplatePayload>> Projection
        {
            get
            {
                return entity => new Domain.Entity.TemplatePayload
                {
                    TemplateId = entity.TemplateId,
                    JsonPayload = entity.JsonPayload,
                    Details = entity.Details.Select(e => AddTemplateDetails.Create(e)).ToList()
                };
            }
        }

        public static Domain.Entity.TemplatePayload Create(AddTemplatePayload model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
