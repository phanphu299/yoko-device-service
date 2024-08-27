using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockFunctionCategory.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockFunctionCategory.Command.Handler
{
    public class GetBlockCategoryByIdHandler : IRequestHandler<GetBlockCategoryById, GetBlockCategoryDto>
    {
        private readonly IBlockCategoryService _blockCategoryService;

        public GetBlockCategoryByIdHandler(IBlockCategoryService blockCategoryService)
        {
            _blockCategoryService = blockCategoryService;
        }

        public Task<GetBlockCategoryDto> Handle(GetBlockCategoryById request, CancellationToken cancellationToken)
        {
            return _blockCategoryService.GetBlockCategoryByIdAsync(request , cancellationToken);
        }
    }
}
