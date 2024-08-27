using System.Threading;
using System.Threading.Tasks;
using Device.Application.Models;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.AssetTemplate.Command.Handler
{
    public class ExportAssetTemplateAttributeRequestHandler : IRequestHandler<ExportAssetTemplateAttribute, ActivityResponse>
    {
        private readonly IAssetTemplateService _service;

        public ExportAssetTemplateAttributeRequestHandler(IAssetTemplateService service)
        {
            _service = service;
        }

        public Task<ActivityResponse> Handle(ExportAssetTemplateAttribute request, CancellationToken cancellationToken)
        {
            return _service.ExportAssetTemplateAttributeAsync(request, cancellationToken);
        }
    }
}
