using System.Threading.Tasks;
using Device.Application.SignalQuality.Command.Model;

namespace Device.Application.Service.Abstraction
{
    public interface IDeviceSignalQualityService
    {
        Task<SignalQualityDto[]> GetAllSignalQualityAsync();
    }
}