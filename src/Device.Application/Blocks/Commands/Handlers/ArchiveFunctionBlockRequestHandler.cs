using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Block.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Block.Command.Handler
{
    public class ArchiveFunctionBlockRequestHandler : IRequestHandler<ArchiveFunctionBlock, IEnumerable<ArchiveFunctionBlockDto>>
    {
        private readonly IFunctionBlockService _functionBlockService;

        public ArchiveFunctionBlockRequestHandler(IFunctionBlockService functionBlockService)
        {
            _functionBlockService = functionBlockService;
        }

        public Task<IEnumerable<ArchiveFunctionBlockDto>> Handle(ArchiveFunctionBlock request, CancellationToken cancellationToken)
        {
            return _functionBlockService.ArchiveAsync(request, cancellationToken);
        }
    }
}
