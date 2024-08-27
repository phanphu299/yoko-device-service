using System;
using Device.Application.BlockFunction.Model;
using MediatR;

namespace Device.Application.Asset.Command
{
    public class AssetAttributeQuery : IRequest<BlockQueryResult>
    {
        public Guid AssetId { get; set; }
        public Guid AttributeId { get; set; }
        public string Method { get; set; }
        public string Padding { get; set; } = "left";
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public string FilterOperation { get; set; }
        public object FilterValue { get; set; }
        public string FilterUnit { get; set; }
        public string Aggregate { get; set; } = "avg";

    }
}