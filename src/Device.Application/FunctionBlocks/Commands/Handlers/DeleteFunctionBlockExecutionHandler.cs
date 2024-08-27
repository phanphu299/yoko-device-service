using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.FunctionBlock.Command.Handler
{
    public class DeleteFunctionBlockExecutionHandler : IRequestHandler<DeleteFunctionBlockExecution, BaseResponse>
    {
        private readonly IFunctionBlockExecutionService _service;
        public DeleteFunctionBlockExecutionHandler(IFunctionBlockExecutionService service)
        {
            _service = service;
        }
        public Task<BaseResponse> Handle(DeleteFunctionBlockExecution request, CancellationToken cancellationToken)
        {
            return _service.DeleteFunctionBlockExecutionAsync(request, cancellationToken);
        }
    }
}
