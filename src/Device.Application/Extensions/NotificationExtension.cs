using System.Threading.Tasks;
using AHI.Infrastructure.Audit.Service.Abstraction;
using Device.Application.Models;

namespace Device.ApplicationExtension.Extension
{
    public static class NotificationExtension
    {
        private const string ASSET_NOTIFY_ENDPOINT = "ntf/notifications/asset/notify";
        private const string ASSET_LIST_NOTIFY_ENDPOINT = "ntf/notifications/asset/list/notify";
        private const string LOCK_NOTIFY_ENDPOINT = "ntf/notifications/entity/lock/notify";

        public static Task SendAssetNotifyAsync(this INotificationService notificationService, AssetNotificationMessage message)
        {
            return notificationService.SendNotifyAsync(ASSET_NOTIFY_ENDPOINT, message);
        }

        public static Task SendAssetListNotifyAsync(this INotificationService notificationService, AssetListNotificationMessage message)
        {
            return notificationService.SendNotifyAsync(ASSET_LIST_NOTIFY_ENDPOINT, message);
        }

        public static Task SendLockNotifyAsync(this INotificationService notificationService, LockNotificationMessage message)
        {
            return notificationService.SendNotifyAsync(LOCK_NOTIFY_ENDPOINT, message);
        }
    }
}