using System.Threading;
using System.Threading.Tasks;
using Device.Application.Template.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Template.Command.Handler
{
    public class GetTemplateByIdRequestHandler : IRequestHandler<GetTemplateByID, GetTemplateDto>
    {
        private readonly IDeviceTemplateService _service;
        public GetTemplateByIdRequestHandler(IDeviceTemplateService service)
        {
            _service = service;
        }

        public Task<GetTemplateDto> Handle(GetTemplateByID request, CancellationToken cancellationToken)
        {
            return _service.FindEntityByIdAsync(request, cancellationToken);
        }
    }
}
