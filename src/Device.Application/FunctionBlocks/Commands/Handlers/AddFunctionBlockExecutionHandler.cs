using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockFunction.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockFunction.Query.Handler
{
    public class AddFunctionBlockExecutionHandler : IRequestHandler<AddFunctionBlockExecution, FunctionBlockExecutionDto>
    {
        private readonly IFunctionBlockExecutionService _blockFunctionService;
        public AddFunctionBlockExecutionHandler(IFunctionBlockExecutionService blockFunctionService)
        {
            _blockFunctionService = blockFunctionService;
        }

        public Task<FunctionBlockExecutionDto> Handle(AddFunctionBlockExecution request, CancellationToken cancellationToken)
        {
            return _blockFunctionService.AddFunctionBlockExecutionAsync(request, cancellationToken);
        }
    }
}