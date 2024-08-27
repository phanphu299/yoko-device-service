using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.Block.Command.Model;
using Device.Application.BlockFunction.Trigger.Model;
using Device.Application.Constant;
using Newtonsoft.Json;

namespace Device.Application.BlockTemplate.Command.Model
{
    public class GetFunctionBlockTemplateDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string DesignContent { get; set; }
        public string Content { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public string CreatedBy { get; set; }
        public string TriggerType { get; set; }
        public string TriggerContent { get; set; }
        public string TriggerAssetMarkup { get; set; }
        public string TriggerAttributeName { get; set; }
        public Guid Version { get; set; }
        public IEnumerable<GetFunctionBlockDto> Inputs { get; set; } = new List<GetFunctionBlockDto>();
        public IEnumerable<GetFunctionBlockDto> Outputs { get; set; } = new List<GetFunctionBlockDto>();
        public IEnumerable<GetFunctionBlockDto> FunctionBlocks { get; set; } = new List<GetFunctionBlockDto>();
        public IEnumerable<FunctionBlockTemplateNodeDto> Nodes { get; set; } = new List<FunctionBlockTemplateNodeDto>();
        private static Func<Domain.Entity.FunctionBlockTemplate, GetFunctionBlockTemplateDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.FunctionBlockTemplate, GetFunctionBlockTemplateDto>> Projection
        {
            get
            {
                return entity => new GetFunctionBlockTemplateDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    DesignContent = entity.DesignContent,
                    CreatedUtc = entity.CreatedUtc,
                    UpdatedUtc = entity.UpdatedUtc,
                    CreatedBy = entity.CreatedBy,
                    Content = entity.Content,
                    Inputs = entity.Nodes.Where(x => x.BlockType == BlockTypeConstants.TYPE_INPUT_CONNECTOR && !x.FunctionBlock.Deleted).Select(x => GetFunctionBlockDto.Create(x.FunctionBlock)),
                    Outputs = entity.Nodes.Where(x => x.BlockType == BlockTypeConstants.TYPE_OUTPUT_CONNECTOR && !x.FunctionBlock.Deleted).Select(x => GetFunctionBlockDto.Create(x.FunctionBlock)),
                    FunctionBlocks = entity.Nodes.Where(x => x.BlockType == BlockTypeConstants.TYPE_BLOCK && !x.FunctionBlock.Deleted).OrderBy(x => x.SequentialNumber).Select(x => GetFunctionBlockDto.Create(x.FunctionBlock)),
                    TriggerContent = entity.TriggerContent,
                    TriggerType = entity.TriggerType,
                    TriggerAssetMarkup = entity.TriggerContent != null ? JsonConvert.DeserializeObject<AssetAttributeTriggerDto>(entity.TriggerContent).AssetMarkup : null,
                    TriggerAttributeName = entity.TriggerContent != null ? JsonConvert.DeserializeObject<AssetAttributeTriggerDto>(entity.TriggerContent).AttributeName : null,
                    Nodes = entity.Nodes.Select(FunctionBlockTemplateNodeDto.Create),
                    Version = entity.Version
                };
            }
        }
        public static GetFunctionBlockTemplateDto Create(Domain.Entity.FunctionBlockTemplate model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
