using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Device.Command
{
    public class VerifyDevice : IRequest<BaseResponse>
    {
        public string Data { get; set; }
    }
}
