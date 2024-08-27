using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.AssetTemplate.Command.Handler
{
    class CheckExistingAssetTemplateRequestHandler : IRequestHandler<CheckExistingAssetTemplate, BaseResponse>
    {
        private readonly IAssetTemplateService _service;
        public CheckExistingAssetTemplateRequestHandler(IAssetTemplateService service)
        {
            _service = service;
        }

        public Task<BaseResponse> Handle(CheckExistingAssetTemplate request, CancellationToken cancellationToken)
        {
            return _service.CheckExistingAssetTemplateAsync(request, cancellationToken);

        }
    }
}
