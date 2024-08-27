using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Block.Command.Handler
{
    public class RetrieveFunctionBlockRequestHandler : IRequestHandler<RetrieveFunctionBlock, BaseResponse>
    {
        private readonly IFunctionBlockService _functionBlockService;

        public RetrieveFunctionBlockRequestHandler(IFunctionBlockService functionBlockService)
        {
            _functionBlockService = functionBlockService;
        }

        public Task<BaseResponse> Handle(RetrieveFunctionBlock request, CancellationToken cancellationToken)
        {
            return _functionBlockService.RetrieveAsync(request, cancellationToken);
        }
    }
}
