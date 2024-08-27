using System;
using System.Collections.Generic;
using MediatR;

namespace Device.Application.Block.Command
{
    public class ValidationFunctionBlocks : IRequest<bool>
    {
        public IEnumerable<Guid> Ids { get; set; }
        public ValidationFunctionBlocks(IEnumerable<Guid> ids)
        {
            Ids = ids;
        }
    }
}
