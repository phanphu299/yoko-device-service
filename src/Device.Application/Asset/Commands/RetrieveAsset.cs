using System.Collections.Generic;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class RetrieveAsset : IRequest<IDictionary<string, object>>
    {
        public string Data { get; set; }
        public string AdditionalData { get; set; }
        public string Upn { get; set; }
    }
}
