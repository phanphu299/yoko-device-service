using System;
using System.Collections.Generic;
using Device.Application.Models;
using MediatR;

namespace Device.Application.FileRequest.Command
{
    public class ExportFile : IRequest<ActivityResponse>
    {
        public Guid ActivityId { get; set; } = Guid.NewGuid();
        public string ObjectType { get; set; }
        public IEnumerable<string> Ids { get; set; }
    }
}
