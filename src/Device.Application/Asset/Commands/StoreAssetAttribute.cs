using System;
using Device.Application.BlockFunction.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class StoreAssetAttribute : IRequest<bool>
    {
        public Guid AssetId { get; set; }
        public Guid AttributeId { get; set; }
        public string AttributeType { get; set; }
        public BlockDataRequest[] Values { get; set; }
    }
}
