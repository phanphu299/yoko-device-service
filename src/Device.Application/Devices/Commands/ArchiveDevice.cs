using System;
using Device.Application.Device.Command.Model;
using MediatR;

namespace Device.Application.Device.Command
{
    public class ArchiveDevice : IRequest<ArchiveDeviceDataDto>
    {
        public DateTime ArchiveTime { get; set; } = DateTime.UtcNow;
    }
}
