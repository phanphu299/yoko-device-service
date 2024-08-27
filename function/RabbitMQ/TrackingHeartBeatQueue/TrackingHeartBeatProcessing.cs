using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using System.Text;
using AHI.Infrastructure.MultiTenancy.Extension;
using System;
using System.Linq;

namespace AHI.Device.Function.Trigger.RabbitMQ
{
    public class TrackingHeartBeatProcessing
    {
        private readonly ITenantContext _tenantContext;
        private readonly IDeviceHeartbeatService _deviceHeartbeatService;
        private readonly IIngestionProcessEventService _ingestionProcessEventService;
        private readonly ILoggerAdapter<TrackingHeartBeatProcessing> _logger;
        public TrackingHeartBeatProcessing(ITenantContext tenantContext, IDeviceHeartbeatService deviceHeartbeatService, IIngestionProcessEventService ingestionProcessEventService, ILoggerAdapter<TrackingHeartBeatProcessing> logger)
        {
            _tenantContext = tenantContext;
            _deviceHeartbeatService = deviceHeartbeatService;
            _ingestionProcessEventService = ingestionProcessEventService;
            _logger = logger;
        }
        [FunctionName("TrackingHeartBeatProcessing")]
        public async Task RunAsync([RabbitMQTrigger("device.function.tracking.heart.beat.processing", ConnectionStringSetting = "RabbitMQ")] byte[] data, ExecutionContext context)
        {
            BaseModel<IngestionMessage> ingestionMessage = null;
            var activityId = Guid.NewGuid().ToString();
            _logger.LogInformation($"[{activityId}] TrackingHeartBeatProcessing - Start processing...");
            try
            {
                ingestionMessage = data.Deserialize<BaseModel<IngestionMessage>>();
                if (ingestionMessage != null)
                {
                    _logger.LogInformation($"[{activityId}] TrackingHeartBeatProcessing - Request: {ingestionMessage.ToJson()}");
                    var trackingHeartBeatDict = ingestionMessage.Message.RawData;
                    var tenantId = trackingHeartBeatDict[Constant.TrackingHeartBeatPayload.TENANT_ID] as string;
                    var subscriptionId = trackingHeartBeatDict[Constant.TrackingHeartBeatPayload.SUBSCRIPTION_ID] as string;
                    var projectId = trackingHeartBeatDict[Constant.TrackingHeartBeatPayload.PROJECT_ID] as string;
                    _tenantContext.RetrieveFromString(tenantId, subscriptionId, projectId);

                    if (!trackingHeartBeatDict.ContainsKey(Constant.TrackingHeartBeatPayload.INTEGRATION_ID))
                    {
                        _logger.LogInformation($"[{activityId}] TrackingHeartBeatProcessing - Getting device(s)...");
                        var listDeviceInformation = await _ingestionProcessEventService.GetListDeviceInformationAsync(trackingHeartBeatDict, projectId);

                        _logger.LogInformation($"[{activityId}] TrackingHeartBeatProcessing - Returned device(s): {listDeviceInformation.ToJson()}");
                        var tasks = listDeviceInformation.Select(deviceInformation => TrackingHeartbeatAsync(projectId, deviceInformation));
                        await Task.WhenAll(tasks);
                    }
                }
            }
            catch (Newtonsoft.Json.JsonReaderException exc)
            {
                _logger.LogError(exc, Encoding.UTF8.GetString(data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, Encoding.UTF8.GetString(data));
            }
            _logger.LogInformation($"[{activityId}] TrackingHeartBeatProcessing - Completed!");
        }

        private async Task TrackingHeartbeatAsync(string projectId, DeviceInformation deviceInformation)
        {
            _logger.LogInformation($"TrackingHeartBeatProcessing - Tracking for deviceId {deviceInformation.DeviceId}");
            await _deviceHeartbeatService.TrackingHeartbeatAsync(projectId, deviceInformation.DeviceId);
            _logger.LogInformation($"TrackingHeartBeatProcessing - Tracked for deviceId {deviceInformation.DeviceId}");
        }
    }
}
