using System;
using System.Threading.Tasks;
using Device.Job.Service.Abstraction;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Audit.Model;

namespace Device.Job.Service
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

        public async Task<ImportExportNotifyPayload> SendFinishExportNotifyAsync(ActionStatus status, string applicationId, bool isExport = true)
        {
            var message = new ExportNotifyPayload(ActivityId, ObjectType, DateTime.UtcNow, NotificationType, status)
            {
                URL = URL,
                Description = GetFinishExportNotifyDescription(status, isExport)
            };
            await _notificationService.SendNotifyAsync(NotifyEndpoint, new UpnNotificationMessage(ObjectType, Upn, applicationId, message));
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
    }
}
