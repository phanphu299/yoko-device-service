using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockTemplate.Query.Handler
{
    public class VerifyArchiveBlockTemplateHandler : IRequestHandler<VerifyArchiveBlockTemplate, BaseResponse>
    {
        private readonly IFunctionBlockTemplateService _functionBlockTemplateService;
        public VerifyArchiveBlockTemplateHandler(IFunctionBlockTemplateService functionBLockTemplateService)
        {
            _functionBlockTemplateService = functionBLockTemplateService;
        }

        public Task<BaseResponse> Handle(VerifyArchiveBlockTemplate request, CancellationToken cancellationToken)
        {
            return _functionBlockTemplateService.VerifyArchiveAsync(request, cancellationToken);
        }
    }
}