using Device.Application.Service.Abstraction;
using MediatR;
using System.Threading.Tasks;
using System.Threading;

namespace Device.Application.BlockTemplate.Command.Handler
{
    public class ValidationBlockTemplatesHandler : IRequestHandler<ValidationBlockTemplates, bool>
    {
        private readonly IFunctionBlockTemplateService _functionBLockTemplateService;
        public ValidationBlockTemplatesHandler(IFunctionBlockTemplateService functionBLockTemplateService)
        {
            _functionBLockTemplateService = functionBLockTemplateService;
        }

        public Task<bool> Handle(ValidationBlockTemplates request, CancellationToken cancellationToken)
        {
            return _functionBLockTemplateService.ValidationBlockTemplateAsync(request, cancellationToken);
        }
    }
}

