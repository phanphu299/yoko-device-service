using System.Threading;
using System.Threading.Tasks;
using Device.Application.Asset.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    public class ParseAssetAttributeRequestHandler : IRequestHandler<ParseAssetAttributes, AssetAttributeParsedResponse>
    {
        private readonly IAssetService _service;

        public ParseAssetAttributeRequestHandler(IAssetService service)
        {
            _service = service;
        }

        public Task<AssetAttributeParsedResponse> Handle(ParseAssetAttributes request, CancellationToken cancellationToken)
        {
            return _service.ParseAssetAttributesAsync(request, cancellationToken);
        }
    }
}
