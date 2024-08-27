using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Device.Application.Asset.Command.Handler
{
    public class VerifyArchiveAssetRequestHandler : IRequestHandler<VerifyArchivedAsset, BaseResponse>
    {
        private readonly IAssetService _service;

        public VerifyArchiveAssetRequestHandler(IAssetService service)
        {
            _service = service;
        }
        public Task<BaseResponse> Handle(VerifyArchivedAsset request, CancellationToken cancellationToken)
        {
            return _service.VerifyArchiveAsync(request,cancellationToken);
        }
    }
}

