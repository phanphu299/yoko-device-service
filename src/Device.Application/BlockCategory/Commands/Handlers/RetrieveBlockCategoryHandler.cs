using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockFunctionCategory.Command.Handler
{
    public class RetrieveBlockCategoryHandler : IRequestHandler<RetrieveBlockCategory, BaseResponse>
    {
        private readonly IBlockCategoryService _service;

        public RetrieveBlockCategoryHandler(IBlockCategoryService service)
        {
            _service = service;
        }

        public Task<BaseResponse> Handle(RetrieveBlockCategory request, CancellationToken cancellationToken)
        {
            return _service.RetrieveAsync(request, cancellationToken);
        }
    }
}