using System.Threading;
using System.Threading.Tasks;
using Device.Application.Block.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Block.Command.Handler
{
    public class AddFunctionBlockHandler : IRequestHandler<AddFunctionBlock, AddFunctionBlockDto>
    {
        private readonly IFunctionBlockService _functionBlockService;
        public AddFunctionBlockHandler(IFunctionBlockService functionBlockService)
        {
            _functionBlockService = functionBlockService;
        }
        public Task<AddFunctionBlockDto> Handle(AddFunctionBlock request, CancellationToken cancellationToken)
        {
            return _functionBlockService.AddEntityAsync(request, cancellationToken);
        }
    }
}
