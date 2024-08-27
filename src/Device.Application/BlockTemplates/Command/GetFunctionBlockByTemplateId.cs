using System;
using System.Collections.Generic;
using Device.Application.Block.Command.Model;
using MediatR;

namespace Device.Application.BlockTemplate.Command
{
    public class GetFunctionBlockByTemplateId : IRequest<IEnumerable<GetFunctionBlockDto>>
    {
        public Guid Id { get; set; }
        public GetFunctionBlockByTemplateId(Guid id)
        {
            Id = id;
        }
    }
}
