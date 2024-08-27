using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;

namespace AHI.Device.Function.Trigger.RabbitMQ
{
    public class RabbitMQMessageLogWriter
    {
        private static string[] POSSIBLE_DEVICE_IDS = new string[]
        {
            "deviceId", "device_id", "DeviceId", "deviceid",
            "entityId", "device_name", "resource", "data.deviceId",
            "tag", "TAG", "payload.data.deviceId", "payload.deviceId",
            "end_device_ids.dev_eui", "metadata.onlineAnalyticId",
            "0.AXIS_TYPE", "DevEUI_uplink.DevEUI"
        };

        private readonly ILogService _logService;

        public RabbitMQMessageLogWriter(ILogService logService)
        {
            _logService = logService;
        }

        [FunctionName("RabbitMQMessageLogWriter")]
        public async Task RunAsync(
        [RabbitMQTrigger("ingestion-log-writer", ConnectionStringSetting = "RabbitMQ")] byte[] data)
        {
            BaseModel<IngestionMessage> ingestionMessage = null;

            try
            {
                ingestionMessage = data.Deserialize<BaseModel<IngestionMessage>>();
            }
            catch { }

            if (ingestionMessage != null)
            {
                var metricDict = ingestionMessage.Message.RawData;
                var projectId = metricDict[MetricPayload.PROJECT_ID] as string;
                var deviceId = "unknown";
                // var foundDeviceId = false;

                foreach (var key in POSSIBLE_DEVICE_IDS)
                {
                    if (metricDict.ContainsKey(key))
                    {
                        deviceId = metricDict[key]?.ToString();
                        // foundDeviceId = true;
                        break;
                    }
                }

                // Comment to prevent loss data due to deviceId key not in the list POSSIBLE_DEVICE_IDS
                // if (!foundDeviceId)
                // {
                //     _logger.LogError($"Cannot found the deviceId for the payload {JsonConvert.SerializeObject(ingestionMessage)}");
                // }

                await _logService.LogMessageAsync(projectId, deviceId, ingestionMessage.ToJson());
            }
        }
    }
}