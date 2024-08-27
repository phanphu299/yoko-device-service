using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.AssetTemplate.Command
{
    public class VerifyAssetTemplate : IRequest<BaseResponse>
    {
        public string Data { get; set; }
    }
}
