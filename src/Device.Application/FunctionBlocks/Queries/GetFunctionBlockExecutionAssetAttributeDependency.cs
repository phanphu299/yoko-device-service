using Device.Application.BlockFunction.Model;
using MediatR;
using System;
using System.Collections.Generic;

namespace Device.Application.BlockFunction.Query
{
    public class GetFunctionBlockExecutionAssetAttributeDependency : IRequest<IEnumerable<FunctionBlockExecutionAssetAttributeDto>>
    {
        public Guid[] AttributeIds { get; set; }
        public GetFunctionBlockExecutionAssetAttributeDependency(Guid[] attributeIds)
        {
            AttributeIds = attributeIds;
        }
    }
}
