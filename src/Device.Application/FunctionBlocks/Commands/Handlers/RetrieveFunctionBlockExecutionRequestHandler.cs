using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockFunction.Query.Handler
{
    public class RetrieveFunctionBlockExecutionRequestHandler : IRequestHandler<RetrieveFunctionBlockExecution, BaseResponse>
    {
        private readonly IFunctionBlockExecutionService _functionBlockExecutionService;

        public RetrieveFunctionBlockExecutionRequestHandler(IFunctionBlockExecutionService functionBlockExecutionService)
        {
            _functionBlockExecutionService = functionBlockExecutionService;
        }

        public Task<BaseResponse> Handle(RetrieveFunctionBlockExecution request, CancellationToken cancellationToken)
        {
            return _functionBlockExecutionService.RetrieveAsync(request, cancellationToken);
        }
    }
}
