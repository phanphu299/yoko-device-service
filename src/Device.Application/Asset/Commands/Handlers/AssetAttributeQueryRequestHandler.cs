using System.Threading;
using MediatR;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using Device.Application.BlockFunction.Model;

namespace Device.Application.Asset.Command.Handler
{
    public class AssetAttributeQueryRequestHandler : IRequestHandler<AssetAttributeQuery, BlockQueryResult>
    {
        private readonly IAssetQueryService _service;
        public AssetAttributeQueryRequestHandler(IAssetQueryService service)
        {
            _service = service;
        }
        public Task<BlockQueryResult> Handle(AssetAttributeQuery request, CancellationToken cancellationToken)
        {
            return _service.QueryAsync(request, cancellationToken);
        }
    }
}
