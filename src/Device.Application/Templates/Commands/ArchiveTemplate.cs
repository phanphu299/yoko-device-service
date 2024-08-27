using System;
using MediatR;
using Device.Application.Template.Command.Model;
using System.Collections.Generic;

namespace Device.Application.Template.Command
{
    public class ArchiveTemplate : IRequest<IEnumerable<ArchiveTemplateDto>>
    {
        public DateTime ArchiveTime { get; set; } = DateTime.UtcNow;
    }
}
