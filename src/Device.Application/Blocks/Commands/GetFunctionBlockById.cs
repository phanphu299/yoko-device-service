using System;
using Device.Application.Block.Command.Model;
using MediatR;

namespace Device.Application.Block.Command
{
    public class GetFunctionBlockById : IRequest<GetFunctionBlockDto>
    {
        public Guid Id { get; set; }
        public GetFunctionBlockById(Guid id)
        {
            Id = id;
        }
    }
}
