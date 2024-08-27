using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command;
using Device.Application.Historical.Query.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Historical.Query.Handler
{
    public class GetAssetAttributeHistoricalDataHandler : IRequestHandler<GetAttributeHistoricalData, IEnumerable<HistoricalDataDto>>
    {
        private readonly IAssetTimeSeriesService _service;
        //private readonly IAssetService _assetService;
        public GetAssetAttributeHistoricalDataHandler(IAssetTimeSeriesService service)
        {
            _service = service;
            //_assetService = assetService;
        }

        public async Task<IEnumerable<HistoricalDataDto>> Handle(GetAttributeHistoricalData request, CancellationToken cancellationToken)
        {
            //await _assetService.CheckUserRightPermissionAsync(request.AssetId, privilegeCode: Privileges.Asset.Rights.READ_ASSET, true, cancellationToken);
            return await _service.GetTimeSeriesDataAsync(request, cancellationToken);
        }
    }
}
