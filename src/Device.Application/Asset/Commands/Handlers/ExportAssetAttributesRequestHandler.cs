using System.Threading;
using System.Threading.Tasks;
using Device.Application.Models;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    public class ExportAssetAttributesRequestHandler : IRequestHandler<ExportAssetAttributes, ActivityResponse>
    {
        private readonly IAssetService _assetService;

        public ExportAssetAttributesRequestHandler(IAssetService assetService)
        {
            _assetService = assetService;
        }

        public Task<ActivityResponse> Handle(ExportAssetAttributes request, CancellationToken cancellationToken)
        {
            return _assetService.ExportAttributesAsync(request, cancellationToken);
        }
    }
}
