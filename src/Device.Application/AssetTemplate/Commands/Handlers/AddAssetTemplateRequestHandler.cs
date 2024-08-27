using System.Threading;
using MediatR;
using System.Threading.Tasks;
using Device.Application.AssetTemplate.Command.Model;
using Device.Application.Service.Abstraction;

namespace Device.Application.AssetTemplate.Command.Handler
{
    public class AddAssetTemplateRequestHandler : IRequestHandler<AddAssetTemplate, AddAssetTemplateDto>
    {
        private readonly IAssetTemplateService _service;
        public AddAssetTemplateRequestHandler(IAssetTemplateService service)
        {
            _service = service;
        }

        public Task<AddAssetTemplateDto> Handle(AddAssetTemplate request, CancellationToken cancellationToken)
        {
            return _service.AddAssetTemplateAsync(request, cancellationToken);
        }
    }
}
