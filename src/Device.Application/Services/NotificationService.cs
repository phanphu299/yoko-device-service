using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Device.Application.Constant;
using Newtonsoft.Json;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.Audit.Model;

namespace Device.Application.Service
{
    public class NotificationService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ITenantContext _tenantContext;

        public NotificationService(
            IHttpClientFactory clientFactory, ITenantContext tenantContext)
        {
            _clientFactory = clientFactory;
            _tenantContext = tenantContext;
        }
        public async Task SendMessageAsync(NotificationMessage message)
        {
            using (var notificationService = _clientFactory.CreateClient(HttpClientNames.NOTIFICATION_HUB, _tenantContext))
            {
                var path = DecorateNotificationMessage(message);
                var payload = JsonConvert.SerializeObject(message.Payload, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting);
                message.Payload = payload;
                await notificationService.PostAsync(path, new StringContent(JsonConvert.SerializeObject(message, AHI.Infrastructure.SharedKernel.Extension.Constant.JsonSerializerSetting), Encoding.UTF8, mediaType: "application/json"));
            }
        }

        private string DecorateNotificationMessage(NotificationMessage message)
        {
            switch (message.Type)
            {
                case NotificationType.LOCK_ENTITY:
                    return "ntf/notifications/entity/lock/notify";
                case NotificationType.ASSET_LIST_CHANGE:
                    // message.TargetId = _tenantContext.ProjectId;
                    return "ntf/notifications/asset/list/notify";
                default:
                    // message.AssetId = Guid.Parse(message.TargetId);
                    return "ntf/notifications/asset/notify";

            }
        }
    }
}
