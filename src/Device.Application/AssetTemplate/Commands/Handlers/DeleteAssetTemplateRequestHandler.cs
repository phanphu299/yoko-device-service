using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.AssetTemplate.Command.Handler
{
    class DeleteAssetTemplateRequestHandler : IRequestHandler<DeleteAssetTemplate, BaseResponse>
    {
        private readonly IAssetTemplateService _service;
        public DeleteAssetTemplateRequestHandler(IAssetTemplateService service)
        {
            _service = service;
        }

        public async Task<BaseResponse> Handle(DeleteAssetTemplate request, CancellationToken cancellationToken)
        {
            //just hard deleted
            var result = await _service.RemoveAssetTemplateAsync(request, cancellationToken);
            return new BaseResponse(result, null);
        }
    }
}