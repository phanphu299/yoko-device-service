using System;
using Device.Application.Constant;

namespace Device.Application.Service
{
    public abstract class FunctionBlockOutputBinding
    {
        public Guid AssetId { get; set; }
        public string Type { get; set; } = BindingDataTypeIdConstants.TYPE_ASSET_ATTRIBUTE;
    }
}