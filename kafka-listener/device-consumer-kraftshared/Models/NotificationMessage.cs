using System;
using AHI.Infrastructure.Audit.Model;

namespace Device.Consumer.KraftShared.Model
{
    public class AssetNotificationMessage : NotificationMessage
    {
        public Guid AssetId { get; set; }
        public AssetNotificationMessage(string type, Guid assetId) : base(type, null)
        {
            AssetId = assetId;
        }
    }
}