using System.Threading;
using System.Threading.Tasks;
using Device.Application.AssetTemplate.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.AssetTemplate.Command.Handler
{
    public class UpdateAssetTemplateRequestHandler : IRequestHandler<UpdateAssetTemplate, UpdateAssetTemplateDto>
    {
        private readonly IAssetTemplateService _service;
        public UpdateAssetTemplateRequestHandler(IAssetTemplateService service)
        {
            _service = service;
        }

        public Task<UpdateAssetTemplateDto> Handle(UpdateAssetTemplate request, CancellationToken cancellationToken)
        {
            // persist to database and implement extra logic
            return _service.UpdateAssetTemplateAsync(request, cancellationToken);
        }
    }
}
