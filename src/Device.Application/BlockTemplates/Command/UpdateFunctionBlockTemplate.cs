using System;
using System.Linq.Expressions;
using Device.Application.BlockTemplate.Command.Model;
using MediatR;

namespace Device.Application.BlockTemplate.Query
{
    public class UpdateFunctionBlockTemplate : BaseEditFunctionBlockTemplate, IRequest<FunctionBlockTemplateDto>
    {
        public Guid Id { get; set; }
        public bool RequiredBlockExecutionRefreshing { get; set; } = false;
        public bool HasDiagramChanged { get; set; } = false;

        private static Func<UpdateFunctionBlockTemplate, Domain.Entity.FunctionBlockTemplate> Converter = Projection.Compile();

        private static Expression<Func<UpdateFunctionBlockTemplate, Domain.Entity.FunctionBlockTemplate>> Projection
        {
            get
            {
                return command => new Domain.Entity.FunctionBlockTemplate
                {
                    Id = command.Id,
                    DesignContent = command.DesignContent,
                    Name = command.Name,
                    TriggerType = command.TriggerType,
                    TriggerContent = command.TriggerContent
                };
            }
        }
        public static Domain.Entity.FunctionBlockTemplate Create(UpdateFunctionBlockTemplate command)
        {
            if (command != null)
            {
                return Converter(command);
            }
            return null;
        }
    }
}
