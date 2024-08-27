using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Device.Application.Asset.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    public class GetAssetPathRequestHandler : IRequestHandler<GetAssetPath, IEnumerable<AssetPathDto>>
    {
        private readonly IAssetService _service;

        public GetAssetPathRequestHandler(IAssetService service)
        {
            _service = service;
        }

        public Task<IEnumerable<AssetPathDto>> Handle(GetAssetPath request, CancellationToken cancellationToken)
        {
            return _service.GetPathsAsync(request, cancellationToken);
        }
    }
}
