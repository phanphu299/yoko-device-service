using System.Threading;
using System.Threading.Tasks;
using Device.Application.BlockFunctionCategory.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockFunctionCategory.Command.Handler
{
    public class UpdateBlockCategoryHandler : IRequestHandler<UpdateBlockCategory, BlockCategoryDto>
    {
        private readonly IBlockCategoryService _blockCategoryService;

        public UpdateBlockCategoryHandler(IBlockCategoryService blockCategoryService)
        {
            _blockCategoryService = blockCategoryService;
        }

        public Task<BlockCategoryDto> Handle(UpdateBlockCategory request, CancellationToken cancellationToken)
        {
            return _blockCategoryService.UpdateBlockCategoryAsync(request , cancellationToken);
        }
    }
}
