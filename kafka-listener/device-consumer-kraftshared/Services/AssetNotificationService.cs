using System.Threading.Tasks;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Service.Abstraction;
using AHI.Infrastructure.Audit.Service.Abstraction;

namespace Device.Consumer.KraftShared.Service
{
    public class AssetNotificationService : IAssetNotificationService
    {
        private string NotifyEndpoint = "ntf/notifications/asset/notify";

        private readonly INotificationService _notificationService;
        public AssetNotificationService(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public Task NotifyAssetAsync(AssetNotificationMessage message)
        {
            return _notificationService.SendNotifyAsync(NotifyEndpoint, message);
        }
    }
}