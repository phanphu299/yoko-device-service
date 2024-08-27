using System.Threading;
using MediatR;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using Device.Application.Historical.Query.Model;
using Device.Application.Historical.Query;
using System.Linq;
using System;

namespace Device.Application.Asset.Command.Handler
{
    public class GetAttributeSnapshotRequestHandler : IRequestHandler<GetAttributeSnapshot, HistoricalDataDto>
    {
        private readonly IAssetSnapshotService _service;
        public GetAttributeSnapshotRequestHandler(IAssetSnapshotService service)
        {
            _service = service;
        }

        public async Task<HistoricalDataDto> Handle(GetAttributeSnapshot request, CancellationToken cancellationToken)
        {
            //await _assetService.CheckUserRightPermissionAsync(request.Id, privilegeCode: Privileges.Asset.Rights.READ_ASSET, true, cancellationToken);
            var command = new GetHistoricalData(request.Id, Array.Empty<Guid>()) { UseCache = request.UseCache };
            var snapshot = await _service.GetSnapshotDataAsync(command, cancellationToken);
            return snapshot.FirstOrDefault();
        }
    }
}
