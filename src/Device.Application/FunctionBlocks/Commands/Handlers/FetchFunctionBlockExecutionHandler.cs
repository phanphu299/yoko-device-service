using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockFunction.Model;
using Device.Application.BlockFunction.Query;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Template.Command.Handler
{
    public class FetchFunctionBlockExecutionHandler : IRequestHandler<FetchFunctionBlockExecution, FunctionBlockExecutionDto>
    {
        private readonly IFunctionBlockExecutionService _service;

        public FetchFunctionBlockExecutionHandler(IFunctionBlockExecutionService service)
        {
            _service = service;
        }

        public Task<FunctionBlockExecutionDto> Handle(FetchFunctionBlockExecution request, CancellationToken cancellationToken)
        {
            return _service.FetchAsync(request.Id);
        }
    }
}