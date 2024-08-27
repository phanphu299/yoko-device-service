using System;
using Device.Application.Block.Command.Model;
using MediatR;

namespace Device.Application.Block.Command
{
    public class FetchFunctionBlock : IRequest<GetFunctionBlockSimpleDto>
    {
        public Guid Id { get; set; }

        public FetchFunctionBlock(Guid id)
        {
            Id = id;
        }
    }
}