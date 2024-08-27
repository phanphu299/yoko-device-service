using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockTemplate.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockTemplate.Query.Handler
{
    public class AddFunctionBlockTemplateHandler : IRequestHandler<AddFunctionBlockTemplate, FunctionBlockTemplateDto>
    {
        private readonly IFunctionBlockTemplateService _functionBLockTemplateService;
        public AddFunctionBlockTemplateHandler(IFunctionBlockTemplateService functionBLockTemplateService)
        {
            _functionBLockTemplateService = functionBLockTemplateService;
        }

        public Task<FunctionBlockTemplateDto> Handle(AddFunctionBlockTemplate request, CancellationToken cancellationToken)
        {
            return _functionBLockTemplateService.AddBlockTemplateAsync(request, cancellationToken);
        }
    }
}
