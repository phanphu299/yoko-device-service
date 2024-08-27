using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockSnippet.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockSnippet.Command.Handler
{
    public class AddBlockSnippetHandler : IRequestHandler<AddBlockSnippet, BlockSnippetDto>
    {
        private readonly IBlockSnippetService _blockSnippetService;

        public AddBlockSnippetHandler(IBlockSnippetService blockSnippetService)
        {
            _blockSnippetService = blockSnippetService;
        }

        public Task<BlockSnippetDto> Handle(AddBlockSnippet request, CancellationToken cancellationToken)
        {
            return _blockSnippetService.AddBlockSnippetAsync(request, cancellationToken);
        }
    }
}
