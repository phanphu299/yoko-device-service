using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using Device.Application.Asset.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    public class FetchAssetAttributeRequestHandler : IRequestHandler<FetchAssetAttribute, FetchAssetAttributeDto>
    {
        private readonly IAssetService _service;

        public FetchAssetAttributeRequestHandler(IAssetService service)
        {
            _service = service;
        }

        public async Task<FetchAssetAttributeDto> Handle(FetchAssetAttribute request, CancellationToken cancellationToken)
        {
            try
            {
                var getCommand = new GetAssetById(request.AssetId);
                var asset = await _service.FindAssetByIdAsync(getCommand, cancellationToken);
                var attribute = asset.Attributes.FirstOrDefault(x => x.Id == request.Id);
                return FetchAssetAttributeDto.Create(asset, attribute);
            }
            catch(EntityNotFoundException)
            {
                return null;
            }
        }
    }
}