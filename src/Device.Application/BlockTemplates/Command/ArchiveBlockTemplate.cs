using System;
using System.Collections.Generic;
using Device.Application.BlockTemplate.Command.Model;
using MediatR;

namespace Device.Application.BlockTemplate.Query
{
    public class ArchiveBlockTemplate : IRequest<IEnumerable<ArchiveBlockTemplateDto>>
    {
        public DateTime ArchiveTime { get; set; } = DateTime.UtcNow;

    }
}