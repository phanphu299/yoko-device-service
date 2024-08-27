using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Block.Command.Handler
{
    public class VerifyArchiveFunctionBlockRequestHandler : IRequestHandler<VerifyFunctionBlock, BaseResponse>
    {
        private readonly IFunctionBlockService _functionBlockService;

        public VerifyArchiveFunctionBlockRequestHandler(IFunctionBlockService functionBlockService)
        {
            _functionBlockService = functionBlockService;
        }

        public Task<BaseResponse> Handle(VerifyFunctionBlock request, CancellationToken cancellationToken)
        {
            return _functionBlockService.VerifyArchiveAsync(request, cancellationToken);
        }
    }
}
