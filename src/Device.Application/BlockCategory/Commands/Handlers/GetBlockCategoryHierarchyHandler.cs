using System.Threading.Tasks;
using System.Threading;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.BlockCategory.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockCategory.Command.Handler
{
    public class GetBlockCategoryHierarchyHandler : IRequestHandler<GetBlockCategoryHierarchy, BaseSearchResponse<GetBlockCategoryHierarchyDto>>
    {
        private readonly IBlockCategoryService _blockCategoryService;

        public GetBlockCategoryHierarchyHandler(IBlockCategoryService blockCategoryService)
        {
            _blockCategoryService = blockCategoryService;
        }

        public Task<BaseSearchResponse<GetBlockCategoryHierarchyDto>> Handle(GetBlockCategoryHierarchy request, CancellationToken cancellationToken)
        {
            return _blockCategoryService.HierarchySearchAsync(request, cancellationToken);
        }
    }
}
