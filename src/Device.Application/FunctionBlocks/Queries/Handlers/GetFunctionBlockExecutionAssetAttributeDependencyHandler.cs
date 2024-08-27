using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockFunction.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockFunction.Query.Handler
{
    public class GetFunctionBlockExecutionAssetAttributeDependencyHandler : IRequestHandler<GetFunctionBlockExecutionAssetAttributeDependency, IEnumerable<FunctionBlockExecutionAssetAttributeDto>>
    {
        private readonly IFunctionBlockExecutionService _blockFunctionService;
        public GetFunctionBlockExecutionAssetAttributeDependencyHandler(IFunctionBlockExecutionService blockFunctionService)
        {
            _blockFunctionService = blockFunctionService;
        }

        public Task<IEnumerable<FunctionBlockExecutionAssetAttributeDto>> Handle(GetFunctionBlockExecutionAssetAttributeDependency request, CancellationToken cancellationToken)
        {
            return _blockFunctionService.GetFunctionBlockExecutionDependencyAsync(request.AttributeIds);
        }
    }
}