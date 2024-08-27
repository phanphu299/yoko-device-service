using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.Constants;
using Device.Application.TemplatePayload.Command;
using Device.Application.Template.Command.Model;
using MediatR;
using AHI.Infrastructure.Validation.CustomAttribute;
using Device.Application.TemplateBinding.Command;
using AHI.Infrastructure.Service.Tag.Model;

namespace Device.Application.Template.Command
{
    public class UpdateTemplates : UpsertTagCommand, IRequest<UpdateTemplatesDto>
    {
        [DynamicValidation(RemoteValidationKeys.metric)]
        [DynamicValidation(RemoteValidationKeys.expression)]
        [DynamicValidation(RemoteValidationKeys.payload)]
        public Guid Id { get; set; }
        [DynamicValidation(RemoteValidationKeys.name)]
        public string Name { get; set; }
        public bool Deleted { get; set; }
        public int TotalMetric { get; set; }
        public ICollection<UpdateTemplatePayload> Payloads { get; set; } = new List<UpdateTemplatePayload>();
        public ICollection<UpdateTemplateBinding> Bindings { get; set; } = new List<UpdateTemplateBinding>();
        static Func<UpdateTemplates, Domain.Entity.DeviceTemplate> Converter = Projection.Compile();
        private static Expression<Func<UpdateTemplates, Domain.Entity.DeviceTemplate>> Projection
        {
            get
            {
                return entity => new Domain.Entity.DeviceTemplate
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    Deleted = entity.Deleted,
                    TotalMetric = entity.TotalMetric,
                    Payloads = entity.Payloads.Select(e => UpdateTemplatePayload.Create(e)).ToList()
                };
            }
        }

        public static Domain.Entity.DeviceTemplate Create(UpdateTemplates model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
