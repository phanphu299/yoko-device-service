using System;
using System.Collections.Generic;
using Device.Application.BlockBinding;
using MediatR;

namespace Device.Application.Block.Command
{
    public class ValidationFunctionBlockContent : IRequest<bool>
    {
        public Guid Id { get; set; }
        public string BlockContent { get; set; }
        public IEnumerable<UpdateFunctionBlockBinding> Bindings { get; set; } = new List<UpdateFunctionBlockBinding>();
    }
}
