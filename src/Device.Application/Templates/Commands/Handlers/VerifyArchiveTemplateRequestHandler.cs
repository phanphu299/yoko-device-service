using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Template.Command.Handler
{
    public class VerifyArchiveTemplateRequestHandler : IRequestHandler<VerifyTemplate, BaseResponse>
    {
        private readonly IDeviceTemplateService _service;

        public VerifyArchiveTemplateRequestHandler(IDeviceTemplateService service)
        {
            _service = service;
        }

        public Task<BaseResponse> Handle(VerifyTemplate request, CancellationToken cancellationToken)
        {
            return _service.VerifyArchiveAsync(request, cancellationToken);
        }
    }
}
