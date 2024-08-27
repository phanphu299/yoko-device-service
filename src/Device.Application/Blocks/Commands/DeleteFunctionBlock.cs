using System;
using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Block.Command
{
    public class DeleteFunctionBlock : IRequest<BaseResponse>
    {
        public IEnumerable<Guid> Ids { get; set; }
        public DeleteFunctionBlock()
        {
            Ids = new List<Guid>();
        }
    }
}
