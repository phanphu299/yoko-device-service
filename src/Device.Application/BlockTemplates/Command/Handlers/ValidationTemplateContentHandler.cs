using Device.Application.Service.Abstraction;
using MediatR;
using System.Threading.Tasks;
using System.Threading;

namespace Device.Application.BlockTemplate.Command.Handler
{
    public class ValidationTemplateContentHandler : IRequestHandler<ValidationBlockContent, bool>
    {
        private readonly IFunctionBlockTemplateService _functionBLockTemplateService;
        public ValidationTemplateContentHandler(IFunctionBlockTemplateService functionBLockTemplateService)
        {
            _functionBLockTemplateService = functionBLockTemplateService;
        }

        public Task<bool> Handle(ValidationBlockContent request, CancellationToken cancellationToken)
        {
            return _functionBLockTemplateService.ValidationChangedTemplateAsync(request, cancellationToken);
        }
    }
}

