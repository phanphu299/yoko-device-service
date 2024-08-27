using AHI.Infrastructure.Service.Tag.Model;

namespace Device.Domain.Entity
{
    public class EntityTagDb : EntityTag
    {
        public Uom Uom { get; set; }
        public Asset Asset { get; set; }
        public DeviceTemplate DeviceTemplate { get; set; }
        public AssetTemplate AssetTemplate { get; set; }
        public Device Device { get; set; }
    }
}
