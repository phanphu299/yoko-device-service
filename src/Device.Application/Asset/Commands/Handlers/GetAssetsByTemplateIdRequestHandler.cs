using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Asset.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Asset.Command.Handler
{
    public class GetAssetsByTemplateIdRequestHandler : IRequestHandler<GetAssetsByTemplateId, BaseSearchResponse<GetAssetSimpleDto>>
    {
        private readonly IAssetService _service;
        public GetAssetsByTemplateIdRequestHandler(IAssetService service)
        {
            _service = service;
        }

        public async Task<BaseSearchResponse<GetAssetSimpleDto>> Handle(GetAssetsByTemplateId request, CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow;
            var data = await _service.GetAssetsByTemplateIdAsync(request, cancellationToken);
            return BaseSearchResponse<GetAssetSimpleDto>.CreateFrom(request, (long)DateTime.UtcNow.Subtract(start).TotalMilliseconds, data.Count(), data);

        }
    }
}
