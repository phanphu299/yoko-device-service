using System;
using System.Linq.Expressions;
using AHI.Infrastructure.SharedKernel.Model;
using AHI.Infrastructure.Validation.CustomAttribute;
using Device.Application.Constants;
using MediatR;

namespace Device.Application.BlockBinding
{
    public class UpdateFunctionBlockBinding : IRequest<BaseResponse>
    {
        public Guid Id { get; set; }
        public Guid FunctionBlockId { get; set; }
        [DynamicValidation(RemoteValidationKeys.metric)]
        public string Key { get; set; }
        public string DataType { get; set; }
        [DynamicValidation(RemoteValidationKeys.general)]
        public string DefaultValue { get; set; }
        public string BindingType { get; set; }
        public string Description { get; set; }
        static Func<UpdateFunctionBlockBinding, Domain.Entity.FunctionBlockBinding> Converter = Projection.Compile();
        private static Expression<Func<UpdateFunctionBlockBinding, Domain.Entity.FunctionBlockBinding>> Projection
        {
            get
            {
                return entity => new Domain.Entity.FunctionBlockBinding
                {
                    Id = entity.Id,
                    Key = entity.Key,
                    DataType = entity.DataType,
                    DefaultValue = entity.DefaultValue,
                    BindingType = entity.BindingType,
                    Description = entity.Description,
                    FunctionBlockId = entity.FunctionBlockId
                };
            }
        }
        public static Domain.Entity.FunctionBlockBinding Create(UpdateFunctionBlockBinding model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
