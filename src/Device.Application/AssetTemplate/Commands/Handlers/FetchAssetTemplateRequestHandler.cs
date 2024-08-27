using System.Threading;
using System.Threading.Tasks;
using Device.Application.AssetTemplate.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.AssetTemplate.Command.Handler
{
    public class FetchAssetTemplateRequestHandler : IRequestHandler<FetchAssetTemplate, GetAssetTemplateDto>
    {
        private readonly IAssetTemplateService _service;

        public FetchAssetTemplateRequestHandler(IAssetTemplateService service)
        {
            _service = service;
        }

        public Task<GetAssetTemplateDto> Handle(FetchAssetTemplate request, CancellationToken cancellationToken)
        {
            return _service.FetchAsync(request.Id);
        }
    }
}