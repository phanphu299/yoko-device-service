using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Device.Command
{
    public class DeleteDevice : IRequest<BaseResponse>
    {
        public IEnumerable<string> DeviceIds { get; set; } = new List<string>();
    }
}
