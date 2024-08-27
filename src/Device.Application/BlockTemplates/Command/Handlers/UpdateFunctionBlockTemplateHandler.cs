using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockTemplate.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockTemplate.Query.Handler
{
    public class UpdateFunctionBlockTemplateHandler : IRequestHandler<UpdateFunctionBlockTemplate, FunctionBlockTemplateDto>
    {
        private readonly IFunctionBlockTemplateService _functionBLockTemplateService;
        public UpdateFunctionBlockTemplateHandler(IFunctionBlockTemplateService functionBLockTemplateService)
        {
            _functionBLockTemplateService = functionBLockTemplateService;
        }

        public Task<FunctionBlockTemplateDto> Handle(UpdateFunctionBlockTemplate request, CancellationToken cancellationToken)
        {
            return _functionBLockTemplateService.UpdateBlockTemplateAsync(request, cancellationToken);
        }
    }
}
