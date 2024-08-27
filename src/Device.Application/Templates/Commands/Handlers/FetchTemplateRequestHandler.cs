using System.Threading;
using System.Threading.Tasks;
using Device.Application.Service.Abstraction;
using Device.Application.Template.Command.Model;
using MediatR;

namespace Device.Application.Template.Command.Handler
{
    public class FetchTemplateRequestHandler : IRequestHandler<FetchTemplate, GetTemplateDto>
    {
        private readonly IDeviceTemplateService _service;

        public FetchTemplateRequestHandler(IDeviceTemplateService service)
        {
            _service = service;
        }

        public async Task<GetTemplateDto> Handle(FetchTemplate request, CancellationToken cancellationToken)
        {
            GetTemplateDto result = await _service.FetchAsync(request.Id);
            return result;
        }
    }
}