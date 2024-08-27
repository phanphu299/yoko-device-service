using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Block.Command.Handler
{
    public class ValidationFunctionBlockContentRequestHandler : IRequestHandler<ValidationFunctionBlockContent, bool>
    {
        private readonly IFunctionBlockService _functionBlockService;
        public ValidationFunctionBlockContentRequestHandler(IFunctionBlockService functionBlockService)
        {
            _functionBlockService = functionBlockService;
        }
        public Task<bool> Handle(ValidationFunctionBlockContent request, CancellationToken cancellationToken)
        {
            return _functionBlockService.ValidationIfUsedFunctionBlockIsChangingAsync(request, cancellationToken);
        }

    }
}
