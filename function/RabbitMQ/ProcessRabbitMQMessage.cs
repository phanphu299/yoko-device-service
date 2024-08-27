using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using System.Collections.Generic;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Infrastructure.SharedKernel.Extension;
using System.Text;

namespace AHI.Device.Function.Trigger.RabbitMQ
{
    public class ProcessRabbitMQMessage
    {
        private readonly IIngestionProcessEventService _dataIngestionCommon;
        private readonly ITenantContext _tenantContext;
        private readonly ILoggerAdapter<ProcessRabbitMQMessage> _logger;

        public ProcessRabbitMQMessage(ITenantContext tenantContext, ILoggerAdapter<ProcessRabbitMQMessage> logger, IIngestionProcessEventService dataIngestionCommon, IIntegrationDeviceCalculateRuntimeMetricService integrationDeviceService)
        {
            _tenantContext = tenantContext;
            _logger = logger;
            _dataIngestionCommon = dataIngestionCommon;
        }

        [FunctionName("ProcessRabbitMQMessage")]
        public async Task RunAsync(
        [RabbitMQTrigger("ingestion", ConnectionStringSetting = "RabbitMQ")] byte[] data)
        {
            _logger.LogInformation("Ingestion: StartIngestion");
            BaseModel<IngestionMessage> ingestionMessage = null;
            try
            {
                ingestionMessage = data.Deserialize<BaseModel<IngestionMessage>>();
            }
            catch (Newtonsoft.Json.JsonReaderException exc)
            {
                _logger.LogError(exc, Encoding.UTF8.GetString(data));
                _logger.LogInformation(exc, "Ingestion: EndIngestion Fail");
                return ;
            }
            try
            {
                if (ingestionMessage != null)
                {
                    var metricDict = ingestionMessage.Message.RawData;
                    var tenantId = metricDict[Constant.MetricPayload.TENANT_ID] as string;
                    var subscriptionId = metricDict[Constant.MetricPayload.SUBSCRIPTION_ID] as string;
                    var projectId = metricDict[Constant.MetricPayload.PROJECT_ID] as string;

                    // Check null tenantId and subscriptionId in RetrieveFromString but projectId dont check.
                    _tenantContext.RetrieveFromString(tenantId, subscriptionId, projectId);
                    if (string.IsNullOrWhiteSpace(projectId))
                    {
                        _logger.LogError($"ProcessRabbitMQMessage - Input data ingestion invalid. projectid is null. tenantId={tenantId}, subscriptionId={subscriptionId}, projectId={projectId}");
                        return;
                    }

                    _logger.LogInformation($"Ingestion: Pre-ProcessEvent, projectId = {projectId}");
                    await _dataIngestionCommon.ProcessEventAsync(metricDict);
                    _logger.LogInformation("Ingestion: EndIngestion Ok");
                }
            }
            catch (Exception exc)
            {
                _logger.LogInformation(exc, "Ingestion: EndIngestion Fail");
            }

        }


    }
    public class IngestionMessage
    {
        public IDictionary<string, object> RawData { get; set; }
    }
}