using System.Threading.Tasks;
using AHI.Device.Function.Model;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface IAssetNotificationService
    {
        Task NotifyAssetAsync(AssetNotificationMessage message);
    }
}