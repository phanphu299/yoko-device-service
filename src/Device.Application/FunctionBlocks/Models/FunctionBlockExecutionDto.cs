using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Device.Application.BlockTemplate.Command.Model;

namespace Device.Application.BlockFunction.Model
{
    public class FunctionBlockExecutionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        // public string InputContent { get; set; }
        public Guid? FunctionBlockId { get; set; }
        public Guid? TemplateId { get; set; }
        public string DiagramContent { get; set; }
        public string TriggerType { get; set; }
        public string TriggerContent { get; set; }
        public string Status { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public DateTime? ExecutedUtc { get; set; }
        public string CreatedBy { get; set; }
        public bool RunImmediately { get; set; }
        public string TriggerAssetMarkup { get; set; }
        public Guid? TriggerAssetId { get; set; }
        public Guid? TriggerAttributeId { get; set; }
        public Guid Version { get; set; }
        public SimpleTemplateDto Template { get; set; }
        public SimpleTemplateDto FunctionBlock { get; set; }
        public IEnumerable<FunctionBlockNodeMappingDto> Mappings { get; set; }
        public string TemplateOverlayName { get; set; }
        static Func<Domain.Entity.FunctionBlockExecution, FunctionBlockExecutionDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.FunctionBlockExecution, FunctionBlockExecutionDto>> Projection
        {
            get
            {
                return entity => new FunctionBlockExecutionDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    //InputContent = entity.InputContent,
                    FunctionBlockId = entity.FunctionBlockId,
                    DiagramContent = entity.DiagramContent,
                    TriggerType = entity.TriggerType,
                    TriggerContent = entity.TriggerContent,
                    TemplateId = entity.TemplateId,
                    Status = entity.Status,
                    UpdatedUtc = entity.UpdatedUtc,
                    ExecutedUtc = entity.ExecutedUtc,
                    CreatedBy = entity.CreatedBy,
                    RunImmediately = entity.RunImmediately,
                    TriggerAssetMarkup = entity.TriggerAssetMarkup,
                    TriggerAssetId = entity.TriggerAssetId,
                    TriggerAttributeId = entity.TriggerAttributeId,
                    Mappings = entity.Mappings.Select(FunctionBlockNodeMappingDto.Create),
                    Version = entity.Version,
                    Template = entity.Template != null ? SimpleFunctionBlockTemplateDto.Create(entity.Template) : null,
                    FunctionBlock = entity.FunctionBlock != null ? SimpleFunctionBlockDto.Create(entity.FunctionBlock) : null,
                    TemplateOverlayName = entity.TemplateOverlay != null ? entity.TemplateOverlay.Name : null,
                };
            }
        }

        public static FunctionBlockExecutionDto Create(Domain.Entity.FunctionBlockExecution entity)
        {
            if (entity != null)
                return Converter(entity);
            return null;
        }
    }
}
