using System;
using System.Threading.Tasks;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Model;

namespace Device.Consumer.KraftShared.Service.Abstraction
{
    public interface IImportNotificationService
    {
        Guid ActivityId { get; set; }
        string ObjectType { get; set; }
        ActionType NotificationType { get; set; }
        string Upn { get; set; }
        Task SendStartNotifyAsync(int count);
        Task<ImportExportNotifyPayload> SendFinishImportNotifyAsync(ActionStatus status, (int Success, int Fail) partialInfo);
    }
}
