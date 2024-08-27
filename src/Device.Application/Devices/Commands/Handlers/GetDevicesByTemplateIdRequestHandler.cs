using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class GetDevicesByTemplateIdRequestHandler : IRequestHandler<GetDevicesByTemplateId, GetDevicesByTemplateIdDto>
    {
        private readonly IDeviceService _service;
        public GetDevicesByTemplateIdRequestHandler(IDeviceService service)
        {
            _service = service;
        }

        public async Task<GetDevicesByTemplateIdDto> Handle(GetDevicesByTemplateId request, CancellationToken cancellationToken)
        {
            var response = await _service.GetDevicesByTemplateIdAsync(request, cancellationToken);
            return new GetDevicesByTemplateIdDto() { TotalCount = response.Count(), Data = response };
        }
    }
}
