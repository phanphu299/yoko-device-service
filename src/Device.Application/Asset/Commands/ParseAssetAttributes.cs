using System.Collections.Generic;
using Device.Application.Asset.Command.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class ParseAssetAttributes : IRequest<AssetAttributeParsedResponse>
    {
        public string ObjectType { get; set; }
        public string FileName { get; set; }
        public string AssetId { get; set; }
        public IEnumerable<ParseAttributeRequest> UnsavedAttributes { get; set; }
    }
}
