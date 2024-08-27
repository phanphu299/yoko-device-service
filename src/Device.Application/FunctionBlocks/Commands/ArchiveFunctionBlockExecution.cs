using System;
using System.Collections.Generic;
using Device.Application.BlockFunction.Model;
using MediatR;

namespace Device.Application.BlockFunction.Query
{
    public class ArchiveFunctionBlockExecution : IRequest<IEnumerable<ArchiveFunctionBlockExecutionDto>>
    {
        public DateTime ArchiveTime { get; set; } = DateTime.UtcNow;
    }
}
