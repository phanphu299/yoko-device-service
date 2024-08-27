using System.Threading;
using System.Threading.Tasks;
using Device.Application.Block.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Block.Command.Handler
{
    public class UpdateFunctionBlockHandler : IRequestHandler<UpdateFunctionBlock, UpdateFunctionBlockDto>
    {
        private readonly IFunctionBlockService _functionBlockService;
        public UpdateFunctionBlockHandler(IFunctionBlockService functionBlockService)
        {
            _functionBlockService = functionBlockService;
        }
        public Task<UpdateFunctionBlockDto> Handle(UpdateFunctionBlock request, CancellationToken cancellationToken)
        {
            return _functionBlockService.UpdateEntityAsync(request, cancellationToken);
        }
    }
}
