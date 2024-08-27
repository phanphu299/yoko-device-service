using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.Template.Command
{
    public class VerifyTemplate : IRequest<BaseResponse>
    {
        public string Data { get; set; }
    }
}
