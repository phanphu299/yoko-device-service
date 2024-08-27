using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Device.Application.BlockFunction.Model;
using MediatR;

namespace Device.Application.BlockFunction.Query
{
    public class AddFunctionBlockExecution : IRequest<FunctionBlockExecutionDto>
    {
        public string Name { get; set; }
        // public string InputContent { get; set; }
        public Guid? TemplateId { get; set; }
        public Guid? FunctionBlockId { get; set; }
        public string DiagramContent { get; set; }
        public string TriggerType { get; set; }
        public string TriggerContent { get; set; }
        public bool RunImmediately { get; set; }
        public AssetMappingDto TriggerMapping { get; set; }
        public IEnumerable<AssetMappingDto> AssetMappings { get; set; } = new List<AssetMappingDto>();
        private static Func<AddFunctionBlockExecution, Domain.Entity.FunctionBlockExecution> Converter = Projection.Compile();
        private static Expression<Func<AddFunctionBlockExecution, Domain.Entity.FunctionBlockExecution>> Projection
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
                    RunImmediately = model.RunImmediately
                };
            }
        }
        public static Domain.Entity.FunctionBlockExecution Create(AddFunctionBlockExecution model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
    public class AssetMappingDto
    {
        public string AssetMarkupName { get; set; }
        public Guid? AssetId { get; set; }
        public Guid? BlockTemplateNodeId { get; set; }
        public string AssetName { get; set; }
    }
}
