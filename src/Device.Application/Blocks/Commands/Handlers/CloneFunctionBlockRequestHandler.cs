using System.Threading;
using System.Threading.Tasks;
using Device.Application.Block.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Block.Command.Handler
{
    public class CloneFunctionBlockRequestHandler : IRequestHandler<GetFunctionBlockClone, GetFunctionBlockDto>
    {
        private readonly IFunctionBlockService _service;
        public CloneFunctionBlockRequestHandler(IFunctionBlockService service)
        {
            _service = service;
        }

        public Task<GetFunctionBlockDto> Handle(GetFunctionBlockClone request, CancellationToken cancellationToken)
        {
            return _service.GetFunctionBlockCloneAsync(request, cancellationToken);
        }
    }
}
