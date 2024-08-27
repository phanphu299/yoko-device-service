using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Template.Command.Handler
{
    public class RetrieveTemplateRequestHandler : IRequestHandler<RetrieveTemplate, BaseResponse>
    {
        private readonly IDeviceTemplateService _service;

        public RetrieveTemplateRequestHandler(IDeviceTemplateService service)
        {
            _service = service;
        }

        public Task<BaseResponse> Handle(RetrieveTemplate request, CancellationToken cancellationToken)
        {
            return _service.RetrieveAsync(request, cancellationToken);
        }
    }
}
