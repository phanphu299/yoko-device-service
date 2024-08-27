using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockFunction.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockFunction.Query.Handler
{
    public class UpdateFunctionBlockExecutionHandler : IRequestHandler<UpdateFunctionBlockExecution, FunctionBlockExecutionDto>
    {
        private readonly IFunctionBlockExecutionService _blockFunctionService;
        public UpdateFunctionBlockExecutionHandler(IFunctionBlockExecutionService blockFunctionService)
        {
            _blockFunctionService = blockFunctionService;
        }

        public Task<FunctionBlockExecutionDto> Handle(UpdateFunctionBlockExecution request, CancellationToken cancellationToken)
        {
            return _blockFunctionService.UpdateFunctionBlockExecutionAsync(request, cancellationToken);
        }
    }
}