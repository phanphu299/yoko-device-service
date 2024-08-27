using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockFunctionCategory.Command.Handler
{
    public class DeleteBlockCategoryHandler : IRequestHandler<DeleteBlockCategory, BaseResponse>
    {
        private readonly IBlockCategoryService _blockCategoryService;

        public DeleteBlockCategoryHandler(IBlockCategoryService blockCategoryService)
        {
            _blockCategoryService = blockCategoryService;
        }

        public Task<BaseResponse> Handle(DeleteBlockCategory request, CancellationToken cancellationToken)
        {
            return _blockCategoryService.DeleteBlockCategoryAsync(request, cancellationToken);
        }
    }
}
