using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockSnippet.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockSnippet.Command.Handler
{
    public class GetBlockSnippetByIdHandler : IRequestHandler<GetBlockSnippetById, BlockSnippetDto>
    {
        private readonly IBlockSnippetService _blockSnippetService;

        public GetBlockSnippetByIdHandler(IBlockSnippetService blockSnippetService)
        {
            _blockSnippetService = blockSnippetService;
        }

        public Task<BlockSnippetDto> Handle(GetBlockSnippetById request, CancellationToken cancellationToken)
        {
            return _blockSnippetService.GetBlockSnippetByIdAsync(request, cancellationToken);
        }
    }
}
