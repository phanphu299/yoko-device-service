using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockFunctionCategory.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockFunctionCategory.Command.Handler
{
    public class AddBlockCategoryHandler : IRequestHandler<AddBlockCategory, BlockCategoryDto>
    {
        private readonly IBlockCategoryService _blockCategoryService;

        public AddBlockCategoryHandler(IBlockCategoryService blockCategoryService)
        {
            _blockCategoryService = blockCategoryService;
        }

        public Task<BlockCategoryDto> Handle(AddBlockCategory request, CancellationToken cancellationToken)
        {
            return _blockCategoryService.AddBlockCategoryAsync(request, cancellationToken);
        }
    }
}
