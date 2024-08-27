using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using Device.Application.Template.Command.Model;
using MediatR;
using System.Collections.Generic;

namespace Device.Application.Template.Command.Handler
{
    public class ArchiveTemplateRequestHandler : IRequestHandler<ArchiveTemplate, IEnumerable<ArchiveTemplateDto>>
    {
        private readonly IDeviceTemplateService _service;

        public ArchiveTemplateRequestHandler(IDeviceTemplateService service)
        {
            _service = service;
        }

        public Task<IEnumerable<ArchiveTemplateDto>> Handle(ArchiveTemplate request, CancellationToken cancellationToken)
        {
            return _service.ArchiveAsync(request, cancellationToken);
        }
    }
}
