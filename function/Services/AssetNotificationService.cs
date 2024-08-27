using System.Threading.Tasks;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.Audit.Service.Abstraction;

namespace AHI.Device.Function.Service
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