using System;
using System.Threading.Tasks;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Model;

namespace Device.Consumer.KraftShared.Service.Abstraction
{
    public interface IExportNotificationService
    {
        Guid ActivityId { get; set; }
        string ObjectType { get; set; }
        ActionType NotificationType { get; set; }
        string Upn { get; set; }
        string URL { get; set; }
        Task SendStartNotifyAsync(int count);
        Task<ImportExportNotifyPayload> SendFinishExportNotifyAsync(ActionStatus status);
    }
}