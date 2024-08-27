using System.Threading;
using MediatR;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.AssetTemplate.Command.Handler
{
    public class RetrieveAssetTemplateRequestHandler : IRequestHandler<RetrieveAssetTemplate, BaseResponse>
    {
        private readonly IAssetTemplateService _service;
        
        public RetrieveAssetTemplateRequestHandler(IAssetTemplateService service)
        {
            _service = service;
        }

        public Task<BaseResponse> Handle(RetrieveAssetTemplate request, CancellationToken cancellationToken)
        {
            return _service.RetrieveAsync(request, cancellationToken);
        }
    }
}
