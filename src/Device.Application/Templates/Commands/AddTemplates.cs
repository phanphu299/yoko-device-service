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
    public class AddTemplates : UpsertTagCommand, IRequest<AddTemplatesDto>, INotification
    {
        [DynamicValidation(RemoteValidationKeys.name)]
        public string Name { get; set; }
        public bool Deleted { get; set; }
        public IEnumerable<AddTemplatePayload> Payloads { get; set; } = new List<AddTemplatePayload>();
        public IEnumerable<AddTemplateBinding> Bindings { get; set; } = new List<AddTemplateBinding>();
        static Func<AddTemplates, Domain.Entity.DeviceTemplate> Converter = Projection.Compile();
        private static Expression<Func<AddTemplates, Domain.Entity.DeviceTemplate>> Projection
        {
            get
            {
                return entity => new Domain.Entity.DeviceTemplate
                {
                    Name = entity.Name,
                    Deleted = entity.Deleted,
                    Payloads = entity.Payloads.Select(e => AddTemplatePayload.Create(e)).ToList()
                };
            }
        }

        public static Domain.Entity.DeviceTemplate Create(AddTemplates model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
