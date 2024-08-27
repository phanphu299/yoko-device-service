using System;
namespace Device.Application.Asset.Command.Model
{
    public class AttributeCommandDetailDto
    {
        public Guid AssetId { get; set; }
        public Guid AttributeId { get; set; }
        public Guid RowVersion { get; set; }
        public string DeviceId { get; set; }
    }
}