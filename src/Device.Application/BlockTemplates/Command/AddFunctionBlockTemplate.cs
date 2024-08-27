using System;
using System.Linq.Expressions;
using Device.Application.BlockTemplate.Command.Model;
using MediatR;

namespace Device.Application.BlockTemplate.Query
{
    public class AddFunctionBlockTemplate : BaseEditFunctionBlockTemplate, IRequest<FunctionBlockTemplateDto>
    {
        private static Func<AddFunctionBlockTemplate, Domain.Entity.FunctionBlockTemplate> Converter = Projection.Compile();
        private static Expression<Func<AddFunctionBlockTemplate, Domain.Entity.FunctionBlockTemplate>> Projection
        {
            get
            {
                return model => new Domain.Entity.FunctionBlockTemplate
                {
                    DesignContent = model.DesignContent,
                    Name = model.Name,
                    TriggerType = model.TriggerType,
                    TriggerContent = model.TriggerContent
                };
            }
        }
        public static Domain.Entity.FunctionBlockTemplate Create(AddFunctionBlockTemplate blockTemplate)
        {
            if (blockTemplate != null)
            {
                return Converter(blockTemplate);
            }
            return null;
        }
    }
}
