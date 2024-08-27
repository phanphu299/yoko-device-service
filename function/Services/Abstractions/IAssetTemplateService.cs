using System.Threading.Tasks;
using AHI.Device.Function.Model;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface IAssetTemplateService
    {
        Task ProcessChangeAsync(AssetTemplateMessage message);
    }
}