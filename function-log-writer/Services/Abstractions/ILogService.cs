using System.Threading.Tasks;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface ILogService
    {
        Task LogMessageAsync(string projectId, string deviceId, string message);
        void DeleteExpiredFiles();
    }
}