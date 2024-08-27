using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Template.Command.Handler
{
    class DeleteTemplateRequestHandler : IRequestHandler<DeleteTemplates, BaseResponse>
    {
        private readonly IDeviceTemplateService _service;
        public DeleteTemplateRequestHandler(IDeviceTemplateService service)
        {
            _service = service;
        }

        public Task<BaseResponse> Handle(DeleteTemplates request, CancellationToken cancellationToken)
        {
            //just hard deleted
            return _service.DeleteEntityAsync(request, cancellationToken);
        }
    }
}
