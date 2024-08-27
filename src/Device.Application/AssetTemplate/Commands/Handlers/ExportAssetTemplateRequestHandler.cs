using System.Threading;
using System.Threading.Tasks;
using Device.Application.Models;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.AssetTemplate.Command.Handler
{
    public class ExportAssetTemplateRequestHandler : IRequestHandler<ExportAssetTemplate, ActivityResponse>
    {
        private readonly IAssetTemplateService _service;

        public ExportAssetTemplateRequestHandler(IAssetTemplateService service)
        {
            _service = service;
        }

        public Task<ActivityResponse> Handle(ExportAssetTemplate request, CancellationToken cancellationToken)
        {
            return _service.ExportAssetTemplateAsync(request, cancellationToken);
        }
    }
}
