using System.Threading;
using System.Threading.Tasks;
using Device.Application.Block.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Block.Command.Handler
{
    public class GetFunctionBlockByIdHandler : IRequestHandler<GetFunctionBlockById, GetFunctionBlockDto>
    {
        private readonly IFunctionBlockService _functionBlockService;
        public GetFunctionBlockByIdHandler(IFunctionBlockService functionBlockService)
        {
            _functionBlockService = functionBlockService;
        }
        public Task<GetFunctionBlockDto> Handle(GetFunctionBlockById request, CancellationToken cancellationToken)
        {
            return _functionBlockService.FindEntityByIdAsync(request, cancellationToken);
        }
    }
}
