using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockSnippet.Command.Handler
{
    public class DeleteBlockSnippetHandler : IRequestHandler<DeleteBlockSnippet, BaseResponse>
    {
        private readonly IBlockSnippetService _blockSnippetService;

        public DeleteBlockSnippetHandler(IBlockSnippetService blockSnippetService)
        {
            _blockSnippetService = blockSnippetService;
        }

        public Task<BaseResponse> Handle(DeleteBlockSnippet request, CancellationToken cancellationToken)
        {
            return _blockSnippetService.DeleteBlockSnippetAsync(request, cancellationToken);
        }
    }
}
