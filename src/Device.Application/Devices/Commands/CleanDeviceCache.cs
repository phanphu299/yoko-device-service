using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class CleanDeviceCache : IRequest<BaseResponse>
    {
        public IEnumerable<string> DeviceIds { get; set; }
        public CleanDeviceCache(IEnumerable<string> deviceIds)
        {
            DeviceIds = deviceIds;
        }
    }
}