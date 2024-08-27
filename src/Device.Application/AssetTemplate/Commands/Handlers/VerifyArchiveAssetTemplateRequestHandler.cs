using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Device.Application.AssetTemplate.Command.Handler
{
    public class VerifyArchiveAssetTemplateRequestHandler : IRequestHandler<VerifyAssetTemplate, BaseResponse>
    {
        private readonly IAssetTemplateService _service;

        public VerifyArchiveAssetTemplateRequestHandler(IAssetTemplateService service)
        {
            _service = service;
        }
        public Task<BaseResponse> Handle(VerifyAssetTemplate request, CancellationToken cancellationToken)
        {
            return _service.VerifyArchiveAsync(request,cancellationToken);
        }
    }
}

