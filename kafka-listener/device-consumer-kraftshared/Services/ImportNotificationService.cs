using System;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Service.Abstraction;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Audit.Model;

namespace Device.Consumer.KraftShared.Service
{
    public class ImportNotificationService : IImportNotificationService
    {
        public Guid ActivityId { get; set; }
        public string ObjectType { get; set; }
        public ActionType NotificationType { get; set; }
        public string Upn { get; set; }
        private string NotifyEndpoint = "ntf/notifications/import/notify";

        private readonly INotificationService _notificationService;
        public ImportNotificationService(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public Task SendStartNotifyAsync(int count)
        {
            var message = new ImportNotifyPayload(ActivityId, ObjectType, DateTime.UtcNow, NotificationType, ActionStatus.Start)
            {
                Description = DescriptionMessage.IMPORT_START,
                Total = count
            };
            return _notificationService.SendNotifyAsync(NotifyEndpoint, new UpnNotificationMessage(ObjectType, Upn, message));
        }

        public async Task<ImportExportNotifyPayload> SendFinishImportNotifyAsync(ActionStatus status, (int Success, int Fail) partialInfo)
        {
            var message = new ImportNotifyPayload(ActivityId, ObjectType, DateTime.UtcNow, NotificationType, status)
            {
                Description = GetFinishImportNotifyDescription(status),
                Success = partialInfo.Success,
                Fail = partialInfo.Fail
            };
            await _notificationService.SendNotifyAsync(NotifyEndpoint, new UpnNotificationMessage(ObjectType, Upn, message));
            return message;
        }

        private string GetFinishImportNotifyDescription(ActionStatus status)
        {
            return status switch
            {
                ActionStatus.Success => DescriptionMessage.IMPORT_SUCCESS,
                ActionStatus.Fail => DescriptionMessage.IMPORT_FAIL,
                ActionStatus.Partial => DescriptionMessage.IMPORT_PARTIAL,
                _ => string.Empty
            };
        }
    }
}
