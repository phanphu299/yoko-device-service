using System;
using System.Collections.Generic;
using MediatR;

namespace Device.Application.BlockTemplate.Command
{
    public class ValidationBlockTemplates : IRequest<bool>
    {
        public IEnumerable<Guid> Ids { get; set; } = new List<Guid>();
        public ValidationBlockTemplates(IEnumerable<Guid> ids)
        {
            Ids = ids;
        }
    }
}
