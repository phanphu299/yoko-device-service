using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockCategory.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockCategory.Command.Handler
{
    public class GetBlockCategoryPathHandler : IRequestHandler<GetBlockCategoryPath, IEnumerable<GetBlockCategoryPathDto>>
    {
        private readonly IBlockCategoryService _blockCategoryService;

        public GetBlockCategoryPathHandler(IBlockCategoryService blockCategoryService)
        {
            _blockCategoryService = blockCategoryService;
        }

        public Task<IEnumerable<GetBlockCategoryPathDto>> Handle(GetBlockCategoryPath request, CancellationToken cancellationToken)
        {
            return _blockCategoryService.GetPathsAsync(request, cancellationToken);
        }
    }
}
