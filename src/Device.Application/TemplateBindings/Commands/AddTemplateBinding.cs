using System;
using System.Linq.Expressions;
using Device.Application.Constants;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;
using AHI.Infrastructure.Validation.CustomAttribute;

namespace Device.Application.TemplateBinding.Command
{
    public class AddTemplateBinding : IRequest<BaseResponse>, INotification
    {
        public Guid TemplateId { get; set; }
        [DynamicValidation(RemoteValidationKeys.metric)]
        public string Key { get; set; }
        public string DataType { get; set; }
        [DynamicValidation(RemoteValidationKeys.general)]
        public string DefaultValue { get; set; }
        static Func<AddTemplateBinding, Domain.Entity.TemplateBinding> Converter = Projection.Compile();
        private static Expression<Func<AddTemplateBinding, Domain.Entity.TemplateBinding>> Projection
        {
            get
            {
                return entity => new Domain.Entity.TemplateBinding
                {
                    TemplateId = entity.TemplateId,
                    Key = entity.Key,
                    DataType = entity.DataType,
                    DefaultValue = entity.DefaultValue
                };
            }
        }
        public static Domain.Entity.TemplateBinding Create(AddTemplateBinding model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
