using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class UpdatedeviceBindingRequestHandler : IRequestHandler<ValidateDeviceBindings, IEnumerable<ValidateDeviceBindingDto>>
    {
        private readonly IAssetService _service;
        public UpdatedeviceBindingRequestHandler(IAssetService service)
        {
            _service = service;
        }

        public Task<IEnumerable<ValidateDeviceBindingDto>> Handle(ValidateDeviceBindings request, CancellationToken cancellationToken)
        {
            return _service.ValidateDeviceBindingAsync(request, cancellationToken);
        }
    }
}
