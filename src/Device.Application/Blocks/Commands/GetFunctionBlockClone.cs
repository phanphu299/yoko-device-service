using System;
using Device.Application.Block.Command.Model;
using MediatR;

namespace Device.Application.Block.Command
{
    public class GetFunctionBlockClone : IRequest<GetFunctionBlockDto>
    {
        public Guid Id { get; set; }
        public GetFunctionBlockClone(Guid id)
        {
            Id = id;
        }
    }
}

