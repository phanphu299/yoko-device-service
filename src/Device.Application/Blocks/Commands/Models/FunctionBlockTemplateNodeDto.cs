using System;
using System.Linq.Expressions;

namespace Device.Application.Block.Command.Model
{
    public class FunctionBlockTemplateNodeDto : FunctionBlockTemplateNodeSimpleDto
    {
        public int SequentialNumber { get; set; } = 1;
        private static Func<Domain.Entity.FunctionBlockTemplateNode, FunctionBlockTemplateNodeDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.FunctionBlockTemplateNode, FunctionBlockTemplateNodeDto>> Projection
        {
            get
            {
                return entity => new FunctionBlockTemplateNodeDto
                {
                    Id = entity.Id,
                    Name = entity.Name,
                    BlockType = entity.BlockType,
                    AssetMarkupName = entity.AssetMarkupName,
                    TargetName = entity.TargetName,
                    PortId = entity.PortId,
                    SequentialNumber = entity.SequentialNumber,
                    Function = GetFunctionBlockDto.Create(entity.FunctionBlock)
                };
            }
        }

        public static new FunctionBlockTemplateNodeDto Create(Domain.Entity.FunctionBlockTemplateNode entity)
        {
            if (entity != null)
            {
                return Converter(entity);
            }
            return null;
        }
    }
}