using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.Exception;
using AHI.Infrastructure.SharedKernel.Model;
using Device.Application.Constant;
using Device.Application.Service.Abstraction;
using MediatR;

namespace Device.Application.BlockFunction.Query.Handler
{
    public class PublishFunctionBlockExecutionHandler : IRequestHandler<PublishFunctionBlockExecution, BaseResponse>
    {
        private readonly IFunctionBlockExecutionService _blockFunctionService;
        public PublishFunctionBlockExecutionHandler(IFunctionBlockExecutionService blockFunctionService)
        {
            _blockFunctionService = blockFunctionService;
        }

        public async Task<BaseResponse> Handle(PublishFunctionBlockExecution request, CancellationToken cancellationToken)
        {
            var isSuccess = await _blockFunctionService.PublishFunctionBlockExecutionAsync(request.Id);
            if (request.ExceptionOnError && !isSuccess)
            {
                throw new GenericProcessFailedException(detailCode: MessageConstants.BLOCK_EXECUTION_HAS_ERROR);
            }
            return BaseResponse.Success;
        }
    }
}