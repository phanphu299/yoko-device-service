using System;
using Device.Application.BlockTemplate.Command.Model;
using MediatR;

namespace Device.Application.BlockTemplate.Query
{
    public class GetBlockTemplateById : IRequest<GetFunctionBlockTemplateDto>
    {
        public Guid Id { get; set; }
        public GetBlockTemplateById(Guid id)
        {
            Id = id;
        }
    }
}
