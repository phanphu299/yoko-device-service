using System;
using System.Linq.Expressions;
using AHI.Infrastructure.SharedKernel.Model;
using AHI.Infrastructure.Validation.CustomAttribute;
using Device.Application.Constants;
using MediatR;

namespace Device.Application.BlockBinding.Command
{
    public class AddFunctionBlockBinding : IRequest<BaseResponse>
    {
        public Guid FunctionBlockId { get; set; }
        [DynamicValidation(RemoteValidationKeys.metric)]
        public string Key { get; set; }
        public string DataType { get; set; }
        [DynamicValidation(RemoteValidationKeys.general)]
        public string DefaultValue { get; set; }
        public string BindingType { get; set; }
        public string Description { get; set; }
        static Func<AddFunctionBlockBinding, Domain.Entity.FunctionBlockBinding> Converter = Projection.Compile();
        private static Expression<Func<AddFunctionBlockBinding, Domain.Entity.FunctionBlockBinding>> Projection
        {
            get
            {
                return entity => new Domain.Entity.FunctionBlockBinding
                {
                    Key = entity.Key,
                    DataType = entity.DataType,
                    DefaultValue = entity.DefaultValue,
                    BindingType = entity.BindingType,
                    Description = entity.Description,
                    FunctionBlockId = entity.FunctionBlockId
                };
            }
        }
        public static Domain.Entity.FunctionBlockBinding Create(AddFunctionBlockBinding model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
