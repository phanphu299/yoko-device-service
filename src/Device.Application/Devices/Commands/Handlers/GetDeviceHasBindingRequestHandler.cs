using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Device.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class GetDeviceHasBindingRequestHandler : IRequestHandler<GetDeviceHasBinding, BaseSearchResponse<GetDeviceDto>>
    {
        private readonly IDeviceService _service;
        public GetDeviceHasBindingRequestHandler(IDeviceService service)
        {
            _service = service;
        }
        public Task<BaseSearchResponse<GetDeviceDto>> Handle(GetDeviceHasBinding request, CancellationToken cancellationToken)
        {
            return _service.FindDeviceHasBinding(request, cancellationToken);
        }
    }
}