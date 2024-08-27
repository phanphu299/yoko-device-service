using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Device.Application.BlockFunction.Model;
using MediatR;

namespace Device.Application.BlockFunction.Query
{
    public class UpdateFunctionBlockExecution : IRequest<FunctionBlockExecutionDto>
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? FunctionBlockId { get; set; }
        public Guid? TemplateId { get; set; }
        public string DiagramContent { get; set; }
        public string TriggerType { get; set; }
        public string TriggerContent { get; set; }
        public AssetMappingDto TriggerMapping { get; set; }
        public IEnumerable<AssetMappingDto> AssetMappings { get; set; } = new List<AssetMappingDto>();
        public bool RunImmediately { get; set; }
        static Func<UpdateFunctionBlockExecution, Domain.Entity.FunctionBlockExecution> Converter = Projection.Compile();
        private static Expression<Func<UpdateFunctionBlockExecution, Domain.Entity.FunctionBlockExecution>> Projection
        {
            get
            {
                return model => new Domain.Entity.FunctionBlockExecution
                {
                    FunctionBlockId = model.FunctionBlockId,
                    DiagramContent = model.DiagramContent,
                    TemplateId = model.TemplateId,
                    Name = model.Name,
                    TriggerType = model.TriggerType,
                    TriggerContent = model.TriggerContent,
                    Id = model.Id,
                    RunImmediately = model.RunImmediately
                };
            }
        }
        public static Domain.Entity.FunctionBlockExecution Create(UpdateFunctionBlockExecution model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}