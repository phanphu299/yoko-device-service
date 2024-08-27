using System.Threading;
using MediatR;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using Device.Application.AssetTemplate.Command.Model;

namespace Device.Application.AssetTemplate.Command.Handler
{
    public class CreateAssetTemplateFromAssetHandler : IRequestHandler<CreateAssetTemplateFromAsset, AddAssetTemplateDto>
    {
        private readonly IAssetTemplateService _service;
        public CreateAssetTemplateFromAssetHandler(IAssetTemplateService service)
        {
            _service = service;
        }

        public Task<AddAssetTemplateDto> Handle(CreateAssetTemplateFromAsset request, CancellationToken cancellationToken)
        {
            return _service.CreateAssetTemplateFromAssetAsync(request, cancellationToken);
        }
    }
}
