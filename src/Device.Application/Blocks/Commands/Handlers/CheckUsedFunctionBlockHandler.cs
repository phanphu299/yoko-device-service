using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Block.Command.Handler
{
    public class CheckUsedFunctionBlockHandler : IRequestHandler<CheckUsedFunctionBlock, BaseResponse>
    {
        private readonly IFunctionBlockService _functionBlockService;

        public CheckUsedFunctionBlockHandler(IFunctionBlockService functionBlockService)
        {
            _functionBlockService = functionBlockService;
        }

        public async Task<BaseResponse> Handle(CheckUsedFunctionBlock request, CancellationToken cancellationToken)
        {
            var result = await _functionBlockService.CheckUsedFunctionBlockAsync(request, cancellationToken);
            return new BaseResponse(result, null);
        }
    }
}