using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockFunctionCategory.Command.Handler
{
    public class VerifyArchiveBlockCategoryHandler : IRequestHandler<VerifyArchiveBlockCategory, BaseResponse>
    {
        private readonly IBlockCategoryService _service;

        public VerifyArchiveBlockCategoryHandler(IBlockCategoryService service)
        {
            _service = service;
        }

        public Task<BaseResponse> Handle(VerifyArchiveBlockCategory request, CancellationToken cancellationToken)
        {
            return _service.VerifyArchiveAsync(request, cancellationToken);
        }
    }
}