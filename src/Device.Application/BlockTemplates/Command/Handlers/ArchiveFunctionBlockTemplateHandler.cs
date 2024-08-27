using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockTemplate.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockTemplate.Query.Handler
{
    public class ArchiveFunctionBlockTemplateHandler : IRequestHandler<ArchiveBlockTemplate, IEnumerable<ArchiveBlockTemplateDto>>
    {
        private readonly IFunctionBlockTemplateService _functionBlockTemplateService;
        public ArchiveFunctionBlockTemplateHandler(IFunctionBlockTemplateService functionBLockTemplateService)
        {
            _functionBlockTemplateService = functionBLockTemplateService;
        }

        public Task<IEnumerable<ArchiveBlockTemplateDto>> Handle(ArchiveBlockTemplate request, CancellationToken cancellationToken)
        {
            return _functionBlockTemplateService.ArchiveAsync(request, cancellationToken);
        }
    }
}