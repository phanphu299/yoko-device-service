using System;
using System.Threading.Tasks;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Audit.Model;
using Device.Application.Service.Abstraction;
using Device.Application.Constant;

namespace Device.Application.Service
{
    public class ExportNotificationService : IExportNotificationService
    {
        public Guid ActivityId { get; set; }
        public string ObjectType { get; set; }
        public ActionType NotificationType { get; set; }
        public string Upn { get; set; }
        public string URL { get; set; }
        private string NotifyEndpoint = "ntf/notifications/export/notify";
        private readonly INotificationService _notificationService;
        public ExportNotificationService(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public Task SendStartNotifyAsync(int count, bool isExport = true)
        {
            var message = new ExportNotifyPayload(ActivityId, ObjectType, DateTime.UtcNow, NotificationType, ActionStatus.Start)
            {
                Description = isExport ? DescriptionMessage.EXPORT_START : DescriptionMessage.DOWNLOAD_START,
                Total = count
            };
            InitializeMessage(message);
            return _notificationService.SendNotifyAsync(NotifyEndpoint, new UpnNotificationMessage(ObjectType, Upn, ApplicationInformation.APPLICATION_ID, message));
        }

        public async Task<ImportExportNotifyPayload> SendFinishExportNotifyAsync(ActionStatus status, bool isExport = true)
        {
            var message = new ExportNotifyPayload(ActivityId, ObjectType, DateTime.UtcNow, NotificationType, status)
            {
                URL = URL,
                Description = GetFinishExportNotifyDescription(status, isExport)
            };
            await _notificationService.SendNotifyAsync(NotifyEndpoint, new UpnNotificationMessage(ObjectType, Upn, ApplicationInformation.APPLICATION_ID, message));
            return message;
        }

        private string GetFinishExportNotifyDescription(ActionStatus status, bool isExport)
        {
            return status switch
            {
                ActionStatus.Success => isExport ? DescriptionMessage.EXPORT_SUCCESS : DescriptionMessage.DOWNLOAD_SUCCESS,
                ActionStatus.Fail => isExport ? DescriptionMessage.EXPORT_FAIL : DescriptionMessage.DOWNLOAD_FAIL,
                _ => string.Empty
            };
        }

        private void InitializeMessage(ExportNotifyPayload message)
        {
            message.CreatedUtc = DateTime.UtcNow;
        }
    }
}
