using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockFunction.Query.Handler
{
    public class VerifyArchiveFunctionBlockExecutionRequestHandler : IRequestHandler<VerifyFunctionBlockExecution, BaseResponse>
    {
        private readonly IFunctionBlockExecutionService _functionBlockExecutionService;

        public VerifyArchiveFunctionBlockExecutionRequestHandler(IFunctionBlockExecutionService functionBlockExecutionService)
        {
            _functionBlockExecutionService = functionBlockExecutionService;
        }

        public Task<BaseResponse> Handle(VerifyFunctionBlockExecution request, CancellationToken cancellationToken)
        {
            return _functionBlockExecutionService.VerifyArchiveAsync(request, cancellationToken);
        }
    }
}
