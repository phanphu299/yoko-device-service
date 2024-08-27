using System.Threading;
using System.Threading.Tasks;
using Device.Application.Asset.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.AssetTemplate.Command.Handler
{
    public class ValidateAssetTemplateRequestHandler : IRequestHandler<ValidateAssetTemplate, ValidateAssetResponse>
    {
        private readonly IAssetTemplateService _service;

        public ValidateAssetTemplateRequestHandler(IAssetTemplateService service)
        {
            _service = service;
        }

        public Task<ValidateAssetResponse> Handle(ValidateAssetTemplate request, CancellationToken cancellationToken)
        {
            return _service.ValidateDeleteTemplateAsync(request.Id);
        }
    }
}
