using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Models.MetricModel;

namespace Device.Consumer.KraftShared.Service.Abstraction
{
//TODO: Enable again once AHI library upgrade to.NET 8
     public interface IFowardingNotificationService
    {
        Task ForwardingNotificationAssetMessageAsync(IngestionMessage ingestionMessage,
            IEnumerable<DeviceInformation> listDeviceInformation,
            Dictionary<string, long> dataUnixTimestamps,
            Dictionary<string, IEnumerable<RuntimeValueObject>> dataRuntimeValues);
        Task ForwardingNotificationAssetMessageAsync(string tenantId, string subscriptionId, string projectId, string deviceId, long unixTimestamp);
        Task SendAssetNotificationMessageAsync(string tenantId, string subscriptionId, string projectId, string deviceIds);
        Task SendAssetNotificationMessageAsync(string tenantId, string subscriptionId, string projectId, IEnumerable<string> deviceIds);
        Task SendAssetNotificationMessageAsync(string tenantId, string subscriptionId, string projectId, IEnumerable<Guid> assetIds);
    }
}
