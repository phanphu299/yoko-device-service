using System.Collections.Generic;
using System.Threading.Tasks;
using AHI.Device.Function.Model;

namespace AHI.Device.Function.Service.Abstraction
{
    public interface IFowardingNotificationService
    {
        Task ForwardingNotificationAssetMessageAsync(IDictionary<string, object> metricDict, IEnumerable<DeviceInformation> listDeviceInformation, Dictionary<string, long> dataUnixTimestamps, Dictionary<string, IEnumerable<RuntimeValueObject>> dataRuntimeValues);
    }
}
