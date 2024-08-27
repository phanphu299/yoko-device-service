using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockFunction.Query.Handler
{
    public class ExecuteFunctionBlockExecutionHandler : IRequestHandler<RunFunctionBlockExecution, bool>
    {
        private readonly IFunctionBlockExecutionService _blockFunctionService;
        public ExecuteFunctionBlockExecutionHandler(IFunctionBlockExecutionService blockFunctionService)
        {
            _blockFunctionService = blockFunctionService;
        }

        public Task<bool> Handle(RunFunctionBlockExecution request, CancellationToken cancellationToken)
        {
            return _blockFunctionService.ExecuteFunctionBlockExecutionAsync(request.Id, request.ExecutionDateTime, request.SnapshotDateTime);
        }
    }
}