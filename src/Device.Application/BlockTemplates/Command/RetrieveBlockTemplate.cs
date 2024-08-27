using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.BlockTemplate.Query
{
    public class RetrieveBlockTemplate : IRequest<BaseResponse>
    {
        public string Data { get; set; }
        public string Upn { get; set; }
    }
}
