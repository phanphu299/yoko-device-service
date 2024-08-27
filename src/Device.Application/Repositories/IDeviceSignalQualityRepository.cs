using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Domain.Entity;

namespace Device.Application.Repository
{
    public interface IDeviceSignalQualityRepository
    {
        Task<IEnumerable<DeviceSignalQuality>> GetAllSignalQualityAsync();
    }
}