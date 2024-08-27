using MediatR;
using AHI.Infrastructure.SharedKernel.Model;

namespace Device.Application.Template.Command
{
    public class RetrieveTemplate : IRequest<BaseResponse>
    {
        public string Data { get; set; }
        public string Upn { get; set; }
    }
}
