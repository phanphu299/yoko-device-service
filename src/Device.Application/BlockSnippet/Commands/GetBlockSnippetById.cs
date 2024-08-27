using System;
using Device.Application.BlockSnippet.Model;
using MediatR;

namespace Device.Application.BlockSnippet.Command
{
    public class GetBlockSnippetById : IRequest<BlockSnippetDto>
    {
        public Guid Id { get; set; }
    }
}
