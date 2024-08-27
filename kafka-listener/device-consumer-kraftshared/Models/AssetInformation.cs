using System;

namespace Device.Consumer.KraftShared.Model
{
    public class AssetInformation
    {
        public Guid AssetId { get; set; }
        public int RetentionDays { get; set; }
    }
}