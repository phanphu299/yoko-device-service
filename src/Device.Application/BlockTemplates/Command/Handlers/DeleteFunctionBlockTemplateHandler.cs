using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockTemplate.Query.Handler
{
    public class DeleteFunctionBlockTemplateHandler : IRequestHandler<DeleteFunctionBlockTemplate, BaseResponse>
    {
        private readonly IFunctionBlockTemplateService _functionBLockTemplateService;
        public DeleteFunctionBlockTemplateHandler(IFunctionBlockTemplateService functionBLockTemplateService)
        {
            _functionBLockTemplateService = functionBLockTemplateService;
        }

        public Task<BaseResponse> Handle(DeleteFunctionBlockTemplate request, CancellationToken cancellationToken)
        {
            return _functionBLockTemplateService.RemoveBlockTemplatesAsync(request, cancellationToken);
        }
    }
}
