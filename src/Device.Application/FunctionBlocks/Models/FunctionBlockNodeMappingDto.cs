using System;
using System.Linq.Expressions;

namespace Device.Application.BlockFunction.Model
{
    public class FunctionBlockNodeMappingDto
    {
        public Guid Id { get; set; }
        public Guid BlockExecutionId { get; set; }
        /// <summary>
        /// If `BlockTemplateNodeId` is null, this entity's storing the using Asset Attribute of `FunctionBlockExecution`'s DiagramContent.
        /// </summary>
        public Guid? BlockTemplateNodeId { get; set; }
        public string AssetMarkupName { get; set; }
        public Guid? AssetId { get; set; }
        public string AssetName { get; set; }
        public string TargetName { get; set; }
        public string Value { get; set; } // value can be asset attribute or asset table or primitive value, we don't know
        private static Func<Domain.Entity.FunctionBlockNodeMapping, FunctionBlockNodeMappingDto> Converter = Projection.Compile();
        private static Expression<Func<Domain.Entity.FunctionBlockNodeMapping, FunctionBlockNodeMappingDto>> Projection
        {
            get
            {
                return entity => new FunctionBlockNodeMappingDto
                {
                    Id = entity.Id,
                    BlockExecutionId = entity.BlockExecutionId,
                    BlockTemplateNodeId = entity.BlockTemplateNodeId,
                    AssetMarkupName = entity.AssetMarkupName,
                    AssetId = entity.AssetId,
                    AssetName = entity.AssetName,
                    TargetName = entity.TargetName,
                    Value = entity.Value
                };
            }
        }

        public static FunctionBlockNodeMappingDto Create(Domain.Entity.FunctionBlockNodeMapping entity)
        {
            if (entity != null)
            {
                return Converter(entity);
            }
            return null;
        }
    }
}
