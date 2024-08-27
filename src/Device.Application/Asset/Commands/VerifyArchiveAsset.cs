using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class VerifyArchivedAsset : IRequest<BaseResponse>
    {
        public string Data { get; set; }
    }
}
