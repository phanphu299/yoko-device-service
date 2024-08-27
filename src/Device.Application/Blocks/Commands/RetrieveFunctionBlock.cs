using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Block.Command
{
    public class RetrieveFunctionBlock : IRequest<BaseResponse>
    {
        public string Data { get; set; }
        public string Upn { get; set; }
    }
}
