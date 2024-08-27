using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using Device.Application.Asset.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    public class FetchAssetRequestHandler : IRequestHandler<FetchAsset, GetAssetSimpleDto>
    {
        private readonly IAssetService _service;

        public FetchAssetRequestHandler(IAssetService service)
        {
            _service = service;
        }

        public async Task<GetAssetSimpleDto> Handle(FetchAsset request, CancellationToken cancellationToken)
        {
            try
            {
                var assetDto = await _service.FindAssetByIdOptimizedAsync(new GetAssetById(request.Id, false), cancellationToken);
                return GetAssetSimpleDto.Create(assetDto);
            }
            catch(EntityValidationException)
            {
                return null;
            }
        }
    }
}