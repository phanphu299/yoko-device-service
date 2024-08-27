using System.Threading;
using System.Threading.Tasks;
using Device.Application.Block.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Block.Command.Handler
{
    public class UpsertFunctionBlockRequestHandler : IRequestHandler<UpsertFunctionBlock, UpsertFunctionBlockDto>
    {
        private readonly IFunctionBlockService _service;
        public UpsertFunctionBlockRequestHandler(IFunctionBlockService service)
        {
            _service = service;
        }

        public Task<UpsertFunctionBlockDto> Handle(UpsertFunctionBlock request, CancellationToken cancellationToken)
        {
            return _service.UpsertFunctionBlockAsync(request, cancellationToken);
        }
    }
}
