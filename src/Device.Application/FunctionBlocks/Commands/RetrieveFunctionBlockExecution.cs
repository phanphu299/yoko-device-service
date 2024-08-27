using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.BlockFunction.Query
{
    public class RetrieveFunctionBlockExecution : IRequest<BaseResponse>
    {
        public string Data { get; set; }
        public string Upn { get; set; }
    }
}
