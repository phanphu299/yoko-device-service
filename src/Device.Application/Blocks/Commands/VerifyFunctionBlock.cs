using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Block.Command
{
    public class VerifyFunctionBlock : IRequest<BaseResponse>
    {
        public string Data { get; set; }
    }
}
