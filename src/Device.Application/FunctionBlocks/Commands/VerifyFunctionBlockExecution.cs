using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.BlockFunction.Query
{
    public class VerifyFunctionBlockExecution : IRequest<BaseResponse>
    {
        public string Data { get; set; }
    }
}
