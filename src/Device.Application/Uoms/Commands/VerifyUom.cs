using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Uom.Command
{
    public class VerifyUom : IRequest<BaseResponse>
    {
        public string Data { get; set; }
    }
}
