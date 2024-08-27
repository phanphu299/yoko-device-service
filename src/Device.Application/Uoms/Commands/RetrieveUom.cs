using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Uom.Command
{
    public class RetrieveUom : IRequest<BaseResponse>
    {
        public string Data { get; set; }
        public string Upn { get; set; }
    }
}
