using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockTemplate.Query.Handler
{
    public class RetrieveFunctionBlockTemplateHandler : IRequestHandler<RetrieveBlockTemplate, BaseResponse>
    {
        private readonly IFunctionBlockTemplateService _functionBlockTemplateService;
        public RetrieveFunctionBlockTemplateHandler(IFunctionBlockTemplateService functionBLockTemplateService)
        {
            _functionBlockTemplateService = functionBLockTemplateService;
        }

        public Task<BaseResponse> Handle(RetrieveBlockTemplate request, CancellationToken cancellationToken)
        {
            return _functionBlockTemplateService.RetrieveAsync(request, cancellationToken);
        }
    }
}