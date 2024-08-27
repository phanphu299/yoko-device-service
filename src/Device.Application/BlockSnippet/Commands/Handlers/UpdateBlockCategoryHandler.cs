using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockSnippet.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockSnippet.Command.Handler
{
    public class UpdateBlockSnippetHandler : IRequestHandler<UpdateBlockSnippet, BlockSnippetDto>
    {
        private readonly IBlockSnippetService _blockSnippetService;

        public UpdateBlockSnippetHandler(IBlockSnippetService blockSnippetService)
        {
            _blockSnippetService = blockSnippetService;
        }

        public Task<BlockSnippetDto> Handle(UpdateBlockSnippet request, CancellationToken cancellationToken)
        {
            return _blockSnippetService.UpdateBlockSnippetAsync(request, cancellationToken);
        }
    }
}
