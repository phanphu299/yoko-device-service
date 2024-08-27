using System;
using AHI.Infrastructure.Audit.Model;
using Device.Application.Constant;

namespace Device.Application.Models
{
    public class AssetNotificationMessage : NotificationMessage
    {
        public Guid AssetId { get; set; }
        
        public AssetNotificationMessage(Guid assetId, string type, object payload) : base(type, payload)
        {
            AssetId = assetId;
        }
    }

    public class LockNotificationMessage : NotificationMessage
    {
        public string TargetId { get; set; }

        public LockNotificationMessage(string targetId, object payload) : base(NotificationType.LOCK_ENTITY, payload)
        {
            TargetId = targetId;
        }
    }

    public class AssetListNotificationMessage : NotificationMessage
    {
        public string TargetId { get; set; }
        
        public AssetListNotificationMessage(string targetId, object payload) : base(NotificationType.ASSET_LIST_CHANGE, payload)
        {
            TargetId = targetId;
        }
    }
}
