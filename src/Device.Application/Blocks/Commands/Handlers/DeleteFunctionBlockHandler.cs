using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Block.Command.Handler
{
    public class DeleteFunctionBlockHandler : IRequestHandler<DeleteFunctionBlock, BaseResponse>
    {
        private readonly IFunctionBlockService _functionBlockService;
        public DeleteFunctionBlockHandler(IFunctionBlockService functionBlockService)
        {
            _functionBlockService = functionBlockService;
        }
        public Task<BaseResponse> Handle(DeleteFunctionBlock request, CancellationToken cancellationToken)
        {
            return _functionBlockService.DeleteEntityAsync(request, cancellationToken);
        }
    }
}
