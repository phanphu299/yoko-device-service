using System.Threading;
using System.Threading.Tasks;
using Device.Application.Asset.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    internal class ValidateAssetRequestHandler : IRequestHandler<ValidateAsset, ValidateAssetResponse>
    {
        private readonly IAssetService _service;

        public ValidateAssetRequestHandler(IAssetService service)
        {
            _service = service;
        }

        public Task<ValidateAssetResponse> Handle(ValidateAsset request, CancellationToken cancellationToken)
        {
            return _service.ValidateDependencyAssetAsync(request, cancellationToken);
        }
    }
}
