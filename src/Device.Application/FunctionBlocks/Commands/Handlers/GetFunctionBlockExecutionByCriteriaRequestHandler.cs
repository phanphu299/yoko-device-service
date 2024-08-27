using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.BlockFunction.Query;
using Device.Application.BlockFunction.Model;
namespace Device.Application.Template.Command.Handler
{
    public class GetFunctionBlockExecutionByCriteriaRequestHandler : IRequestHandler<GetFunctionBlockExecutionByCriteria, BaseSearchResponse<FunctionBlockExecutionDto>>
    {
        private readonly IFunctionBlockExecutionService _service;
        public GetFunctionBlockExecutionByCriteriaRequestHandler(IFunctionBlockExecutionService service)
        {
            _service = service;
        }

        public Task<BaseSearchResponse<FunctionBlockExecutionDto>> Handle(GetFunctionBlockExecutionByCriteria request, CancellationToken cancellationToken)
        {
            return _service.SearchAsync(request);
        }
    }
}
