using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Device.Application.Block.Command.Model;
using Device.Application.BlockFunction.Trigger.Model;
using Newtonsoft.Json;

namespace Device.Application.BlockTemplate.Command.Model
{
    public class FunctionBlockTemplateSimpleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string DesignContent { get; set; }
        public string TriggerType { get; set; }
        public string TriggerContent { get; set; }
        public string TriggerAssetMarkup { get; set; }
        public string TriggerAttributeName { get; set; }
        public Guid Version { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public IEnumerable<FunctionBlockTemplateNodeSimpleDto> Nodes { get; set; } = new List<FunctionBlockTemplateNodeSimpleDto>();

        static Func<Domain.Entity.FunctionBlockTemplate, FunctionBlockTemplateSimpleDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.FunctionBlockTemplate, FunctionBlockTemplateSimpleDto>> Projection
        {
            get
            {
                return model => new FunctionBlockTemplateSimpleDto
                {
                    Id = model.Id,
                    Name = model.Name,
                    UpdatedUtc = model.UpdatedUtc,
                    CreatedUtc = model.CreatedUtc,
                    DesignContent = model.DesignContent,
                    Version = model.Version,
                    Nodes = model.Nodes.Select(FunctionBlockTemplateNodeSimpleDto.Create),
                    TriggerContent = model.TriggerContent,
                    TriggerType = model.TriggerType,
                    TriggerAssetMarkup = model.TriggerContent != null ? JsonConvert.DeserializeObject<AssetAttributeTriggerDto>(model.TriggerContent).AssetMarkup : null,
                    TriggerAttributeName = model.TriggerContent != null ? JsonConvert.DeserializeObject<AssetAttributeTriggerDto>(model.TriggerContent).AttributeName : null,
                };
            }
        }

        public static FunctionBlockTemplateSimpleDto Create(Domain.Entity.FunctionBlockTemplate model)
        {
            if (model != null)
            {
                return Converter(model);
            }
            return null;
        }
    }
}
