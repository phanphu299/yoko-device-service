using System;
using System.Linq.Expressions;

namespace Device.Application.Block.Command.Model
{
    public class FunctionBlockTemplateNodeSimpleDto
    {
        public Guid Id { get; set; }
        public string BlockType { get; set; }
        public string Name { get; set; }
        public string AssetMarkupName { get; set; }
        public string TargetName { get; set; }
        public Guid? PortId { get; set; }
        public GetFunctionBlockSimpleDto Function { get; set; }
        private static Func<Domain.Entity.FunctionBlockTemplateNode, FunctionBlockTemplateNodeSimpleDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.FunctionBlockTemplateNode, FunctionBlockTemplateNodeSimpleDto>> Projection
        {
            get
            {
                return entity => new FunctionBlockTemplateNodeSimpleDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    BlockType = entity.BlockType,
                    AssetMarkupName = entity.AssetMarkupName,
                    TargetName = entity.TargetName,
                    PortId = entity.PortId,
                    Function = GetFunctionBlockSimpleDto.Create(entity.FunctionBlock)
                };
            }
        }

        public static FunctionBlockTemplateNodeSimpleDto Create(Domain.Entity.FunctionBlockTemplateNode entity)
        {
            if (entity != null)
            {
                return Converter(entity);
            }
            return null;
        }
    }
}
