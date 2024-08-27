using System;
using Device.Application.Uom.Command.Model;
using MediatR;

namespace Device.Application.Uom.Command
{
    public class ArchiveUom : IRequest<ArchiveUomDataDto>
    {
        public DateTime ArchiveTime { get; set; } = DateTime.UtcNow;
    }
}
