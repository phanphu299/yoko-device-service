using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Device.Command.Model;
using MediatR;

namespace Device.Application.Device.Command
{
    public class GetDeviceHasBinding : BaseCriteria, IRequest<BaseSearchResponse<GetDeviceDto>>
    {
        public string DeviceId { get; set; }
    }
}