using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.UserContext.Service.Abstraction;
using Device.Application.Asset.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    class GetFullAssetByIdRequestHandler : IRequestHandler<GetFullAssetById, GetFullAssetDto>
    {
        private readonly IAssetService _service;
        private readonly ISecurityService _securityService;
        private readonly ISecurityContext _securityContext;

        public GetFullAssetByIdRequestHandler(IAssetService service, ISecurityService securityService, ISecurityContext securityContext)
        {
            _service = service;
            _securityService = securityService;
            _securityContext = securityContext;
        }

        public async Task<GetFullAssetDto> Handle(GetFullAssetById request, CancellationToken cancellationToken)
        {
            var assetDto = await _service.FindFullAssetByIdAsync(request, cancellationToken);
            return assetDto;
        }
    }
}
