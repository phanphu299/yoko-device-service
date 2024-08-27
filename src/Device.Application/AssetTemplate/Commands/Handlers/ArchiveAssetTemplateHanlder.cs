using System.Threading;
using System.Threading.Tasks;
using Device.Application.AssetTemplate.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.AssetTemplate.Command.Handler
{
    public class ArchiveAssetTemplateHandler : IRequestHandler<ArchiveAssetTemplate, ArchiveAssetTemplateDataDto>
    {
        private readonly IAssetTemplateService _service;
        public ArchiveAssetTemplateHandler(IAssetTemplateService service)
        {
            _service = service;
        }

        public async Task<ArchiveAssetTemplateDataDto> Handle(ArchiveAssetTemplate request, CancellationToken cancellationToken)
        {
            var assetTemplates = await _service.ArchiveAsync(request, cancellationToken);
            var result = new ArchiveAssetTemplateDataDto(assetTemplates);
            return result;
        }
    }
}
