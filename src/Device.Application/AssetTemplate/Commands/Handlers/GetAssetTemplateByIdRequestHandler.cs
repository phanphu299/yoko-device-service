using System.Threading;
using System.Threading.Tasks;
using Device.Application.AssetTemplate.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.AssetTemplate.Command.Handler
{
    class GetAssetTemplateByIdRequestHandle : IRequestHandler<GetAssetTemplateById, GetAssetTemplateDto>
    {
        private readonly IAssetTemplateService _service;
        public GetAssetTemplateByIdRequestHandle(IAssetTemplateService service)
        {
            _service = service;
        }

        public Task<GetAssetTemplateDto> Handle(GetAssetTemplateById request, CancellationToken cancellationToken)
        {
            return _service.FindTemplateByIdAsync(request, cancellationToken);
        }
    }
}
