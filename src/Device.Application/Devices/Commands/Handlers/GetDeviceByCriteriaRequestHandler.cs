using System.Threading;
using System.Threading.Tasks;
using Device.Application.Device.Command.Model;
using Device.Application.Service.Abstraction;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Device.Command.Handler
{
    public class GetDeviceByCriteriaRequestHandler : IRequestHandler<GetDeviceByCriteria, BaseSearchResponse<GetDeviceDto>>
    {
        private readonly IDeviceService _service;
        public GetDeviceByCriteriaRequestHandler(IDeviceService service)
        {
            _service = service;
        }

        public Task<BaseSearchResponse<GetDeviceDto>> Handle(GetDeviceByCriteria request, CancellationToken cancellationToken)
        {
            return _service.SearchAsync(request);
        }
    }
}
