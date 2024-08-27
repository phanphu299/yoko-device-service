using System.Threading;
using MediatR;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using System.Collections.Generic;

namespace Device.Application.Asset.Command.Handler
{
    public class RetrieveAssetRequestHandler : IRequestHandler<RetrieveAsset, IDictionary<string, object>>
    {
        private readonly IAssetService _service;
        public RetrieveAssetRequestHandler(IAssetService service)
        {
            _service = service;
        }

        public Task<IDictionary<string, object>> Handle(RetrieveAsset request, CancellationToken cancellationToken)
        {
            return _service.RetrieveAsync(request, cancellationToken);
        }
    }
}
