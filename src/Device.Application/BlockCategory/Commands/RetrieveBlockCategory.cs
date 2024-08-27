using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.BlockFunctionCategory.Command
{
    public class RetrieveBlockCategory : IRequest<BaseResponse>
    {
        public string Data { get; set; }
        public string Upn { get; set; }
    }
}
