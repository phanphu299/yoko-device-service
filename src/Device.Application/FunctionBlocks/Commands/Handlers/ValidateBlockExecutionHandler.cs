using Device.Application.BlockFunction.Model;
using Device.Application.Service.Abstraction;
using MediatR;
using System.Threading.Tasks;
using System.Threading;

namespace Device.Application.BlockFunction.Query.Handler
{
    internal class ValidateBlockExecutionHandler : IRequestHandler<ValidationBlockExecution, ValidationBlockExecutionDto>
    {
        private readonly IFunctionBlockExecutionService _blockFunctionService;
        public ValidateBlockExecutionHandler(IFunctionBlockExecutionService blockFunctionService)
        {
            _blockFunctionService = blockFunctionService;
        }

        public Task<ValidationBlockExecutionDto> Handle(ValidationBlockExecution request, CancellationToken cancellationToken)
        {
            return _blockFunctionService.ValidateBlockExecutionAsync(request, cancellationToken);
        }
    }
}
