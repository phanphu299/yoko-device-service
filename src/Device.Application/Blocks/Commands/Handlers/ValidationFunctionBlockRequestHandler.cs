using Device.Application.Service.Abstraction;
using System.Threading.Tasks;
using System.Threading;
using MediatR;

namespace Device.Application.Block.Command.Handler
{
    internal class ValidationFunctionBlockRequestHandler : IRequestHandler<ValidationFunctionBlocks, bool>
    {
        private readonly IFunctionBlockService _functionBlockService;
        public ValidationFunctionBlockRequestHandler(IFunctionBlockService functionBlockService)
        {
            _functionBlockService = functionBlockService;
        }
        public Task<bool> Handle(ValidationFunctionBlocks request, CancellationToken cancellationToken)
        {
            return _functionBlockService.ValidationFunctionBlockAsync(request, cancellationToken);
        }
    }
}
