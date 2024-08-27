using System.Threading.Tasks;
using AHI.Device.Function.Model;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.SharedKernel.Model;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface IDataIngestionService
    {
        Task IngestDataAsync(DataIngestionMessage eventMessage);
        Task<BaseResponse> ValidateIngestionDataAsync(string filePath);
        Task SendFileIngestionStatusNotifyAsync(ActionStatus status, string description);
        Task LogActivityAsync(string filePath, ActionStatus logEventStatus);
    }
}
