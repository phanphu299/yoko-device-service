using System;
using Device.Application.BlockTemplate.Command.Model;
using MediatR;

namespace Device.Application.BlockTemplate.Command
{
    public class FetchBlockTemplate : IRequest<FunctionBlockTemplateSimpleDto>
    {
        public Guid Id { get; set; }

        public FetchBlockTemplate(Guid id)
        {
            Id = id;
        }
    }
}