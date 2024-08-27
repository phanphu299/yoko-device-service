using System;
using AHI.Infrastructure.Audit.Model;

namespace AHI.Device.Function.Model
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