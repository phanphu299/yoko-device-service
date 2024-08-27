using System;

namespace Device.Application.BlockFunction.Model
{
    public class FunctionBlockExecutionAssetAttributeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public static FunctionBlockExecutionAssetAttributeDto Create(Domain.Entity.FunctionBlockExecution functionBlock)
        {
            if (functionBlock != null)
            {
                return new FunctionBlockExecutionAssetAttributeDto
                {
                    Id = functionBlock.Id,
                    Name = functionBlock.Name
                };
            }
            return null;
        }
    }
}