using System;
using System.Collections.Generic;
using Device.Application.Block.Command.Model;
using MediatR;

namespace Device.Application.Block.Command
{
    public class ArchiveFunctionBlock : IRequest<IEnumerable<ArchiveFunctionBlockDto>>
    {
        public DateTime ArchiveTime { get; set; } = DateTime.UtcNow;
    }
}
