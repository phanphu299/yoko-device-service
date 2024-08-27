using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockFunction.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockFunction.Query.Handler
{
    public class ArchiveFunctionBlockExecutionRequestHandler : IRequestHandler<ArchiveFunctionBlockExecution, IEnumerable<ArchiveFunctionBlockExecutionDto>>
    {
        private readonly IFunctionBlockExecutionService _functionBlockExecutionService;

        public ArchiveFunctionBlockExecutionRequestHandler(IFunctionBlockExecutionService functionBlockExecutionService)
        {
            _functionBlockExecutionService = functionBlockExecutionService;
        }

        public Task<IEnumerable<ArchiveFunctionBlockExecutionDto>> Handle(ArchiveFunctionBlockExecution request, CancellationToken cancellationToken)
        {
            return _functionBlockExecutionService.ArchiveAsync(request, cancellationToken);
        }
    }
}
