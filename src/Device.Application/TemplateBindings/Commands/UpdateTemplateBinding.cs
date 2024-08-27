
using System;
using System.Linq.Expressions;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.TemplateBinding.Command
{
    public class UpdateTemplateBinding : IRequest<BaseResponse>
    {
        public int Id { get; set; }
        public Guid TemplateId { get; set; }
        public string Key { get; set; }
        public string DataType { get; set; }
        public string DefaultValue { get; set; }
        static Func<UpdateTemplateBinding, Domain.Entity.TemplateBinding> Converter = Projection.Compile();
        private static Expression<Func<UpdateTemplateBinding, Domain.Entity.TemplateBinding>> Projection
        {
            get
            {
                return entity => new Domain.Entity.TemplateBinding
                {
                    Id = entity.Id,
                    TemplateId = entity.TemplateId,
                    Key = entity.Key,
                    DataType = entity.DataType,
                    DefaultValue = entity.DefaultValue
                };
            }
        }

        public static Domain.Entity.TemplateBinding Create(UpdateTemplateBinding model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
