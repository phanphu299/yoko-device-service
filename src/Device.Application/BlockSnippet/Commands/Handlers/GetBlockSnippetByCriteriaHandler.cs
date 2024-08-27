using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.BlockSnippet.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockSnippet.Command.Handler
{
    public class GetBlockSnippetByCriteriaHandler : IRequestHandler<GetBlockSnippetByCriteria, BaseSearchResponse<BlockSnippetDto>>
    {
        private readonly IBlockSnippetService _service;

        public GetBlockSnippetByCriteriaHandler(IBlockSnippetService service)
        {
            _service = service;
        }

        public Task<BaseSearchResponse<BlockSnippetDto>> Handle(GetBlockSnippetByCriteria request, CancellationToken cancellationToken)
        {
            return _service.SearchAsync(request);
        }
    }
}
