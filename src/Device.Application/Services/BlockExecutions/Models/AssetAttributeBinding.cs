using System;
using Device.Application.Asset.Command.Model;

namespace Device.Application.Service
{
    public class AssetAttributeBinding : FunctionBlockOutputBinding, IAssetAttribute
    {
        public Guid? AttributeId { get; set; }
        public DateTime? SnapshotDateTime { get; set; }
        public string AttributeType { get; set; }
        public string AttributeDataType { get; set; }
        public object AttributeStaticValue { get; set; }

        public override string ToString()
        {
            return $"{AssetId}_{AttributeId}";
        }
    }
}