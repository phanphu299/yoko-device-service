using System;

namespace Device.Application.Service
{
    public class AssetTableBinding : FunctionBlockOutputBinding
    {
        public Guid? TableId { get; set; }
    }
}