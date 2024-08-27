using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.BlockTemplate.Query
{
    public class VerifyArchiveBlockTemplate : IRequest<BaseResponse>
    {
        public string Data { get; set; }
    }
}