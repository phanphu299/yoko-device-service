using System;

namespace Device.Domain.Entity
{
    public class AssetWarning
    {
        public Guid AssetId { get; set; }
        public bool HasWarning { get; set; }
        public Asset Asset { get; set; }
    }
}
