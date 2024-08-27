using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Device.Function.Model;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface IIngestionProcessEventService
    {
        Task ProcessEventAsync(IDictionary<string, object> metricDict);

        Task<IEnumerable<DeviceInformation>> GetListDeviceInformationAsync(IDictionary<string, object> metricDict, string projectId);
    }
}
