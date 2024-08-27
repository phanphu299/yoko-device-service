using System.Collections.Generic;
using AHI.Infrastructure.SharedKernel.Model;
using MediatR;

namespace Device.Application.AssetTemplate.Command
{
    public class RetrieveAssetTemplate : IRequest<BaseResponse>
    {
        public string Data { get; set; }
        public IDictionary<string, object> AdditionalData { get; set; }
        public string Upn { get; set; }
    }
}
