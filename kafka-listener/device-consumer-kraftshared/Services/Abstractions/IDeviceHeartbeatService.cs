using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Model;

namespace Device.Consumer.KraftShared.Service.Abstraction
{
    public interface IDeviceHeartbeatService
    {
        // this interface can be extented depend on business needs
        Task<IEnumerable<(string DeviceId, string TenantId, string SubscriptionId, string ProjectId)>> ProcessSignalQualityAsync(ProjectDto project);
        Task TrackingHeartbeatAsync(string projectId, string deviceId);
    }
}
