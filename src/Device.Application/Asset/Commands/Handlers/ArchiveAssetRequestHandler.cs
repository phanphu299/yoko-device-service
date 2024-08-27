using System.Threading;
using System.Threading.Tasks;
using Device.Application.Asset.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    public class ArchiveAssetRequestHandler : IRequestHandler<ArchiveAsset, ArchiveAssetDataDto>
    {
        private readonly IAssetService _service;
        public ArchiveAssetRequestHandler(IAssetService service)
        {
            _service = service;
        }

        public async Task<ArchiveAssetDataDto> Handle(ArchiveAsset request, CancellationToken cancellationToken)
        {
            var assets = await _service.ArchiveAsync(request, cancellationToken);
            var result = new ArchiveAssetDataDto(assets);
            return result;
        }
    }
}
