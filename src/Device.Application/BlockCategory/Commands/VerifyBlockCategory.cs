using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.BlockFunctionCategory.Command
{
    public class VerifyArchiveBlockCategory : IRequest<BaseResponse>
    {
        public string Data { get; set; }
    }
}