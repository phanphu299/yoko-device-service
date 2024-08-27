using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.SharedKernel.Extension;

namespace AHI.Device.Function.Trigger.RabbitMQ
{
    public class DataIngestionProcessing
    {
        private readonly ITenantContext _tenantContext;
        private readonly IDataIngestionService _dataIngestionService;
        private readonly ILoggerAdapter<DataIngestionProcessing> _logger;


        public DataIngestionProcessing(
            ITenantContext tenantContext,
            IDataIngestionService dataIngestionService,
            ILoggerAdapter<DataIngestionProcessing> logger)
        {
            _tenantContext = tenantContext;
            _dataIngestionService = dataIngestionService;
            _logger = logger;
        }

        [FunctionName("DataIngestionProcessing")]
        public async Task RunAsync(
        [RabbitMQTrigger("device.function.file.uploaded.processing", ConnectionStringSetting = "RabbitMQ")] byte[] data, ExecutionContext context)
        {
            var filePath = string.Empty;
            try
            {
                _logger.LogTrace("Ingestion: Start");
                BaseModel<DataIngestionMessage> request = data.Deserialize<BaseModel<DataIngestionMessage>>();
                var eventMessage = request.Message;
                filePath = eventMessage.FilePath;
                // setup Domain to use inside repository
                _tenantContext.SetTenantId(eventMessage.TenantId);
                _tenantContext.SetSubscriptionId(eventMessage.SubscriptionId);
                _tenantContext.SetProjectId(eventMessage.ProjectId);

                await _dataIngestionService.SendFileIngestionStatusNotifyAsync(ActionStatus.Start, Constant.DescriptionMessage.INGEST_START);
                await _dataIngestionService.IngestDataAsync(eventMessage);
            }
            catch (System.Exception e)
            {
                _logger.LogError(e, e.Message);
                await _dataIngestionService.SendFileIngestionStatusNotifyAsync(ActionStatus.Fail, Constant.DescriptionMessage.INGEST_FAIL);
                await _dataIngestionService.LogActivityAsync(filePath, ActionStatus.Fail);
            }
            _logger.LogTrace("Ingestion: End");

        }
    }
}
