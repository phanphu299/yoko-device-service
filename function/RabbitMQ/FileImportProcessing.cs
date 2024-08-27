using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.Audit.Model;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.UserContext.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.SharedKernel.Abstraction;

namespace AHI.Device.Function.Trigger.RabbitMQ
{
    public class FileImportProcessing
    {
        private readonly ITenantContext _tenantContext;
        private readonly IFileImportService _importService;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserContext _userContext;
        private readonly ILoggerAdapter<FileImportProcessing> _logger;

        public FileImportProcessing(ITenantContext tenantContext, IFileImportService importService, IAuditLogService auditLogService, IUserContext userContext, ILoggerAdapter<FileImportProcessing> logger)
        {
            _tenantContext = tenantContext;
            _importService = importService;
            _auditLogService = auditLogService;
            _userContext = userContext;
            _logger = logger;
        }

        [FunctionName("FileImportProcessing")]
        public async Task RunAsync(
        [RabbitMQTrigger("device.function.file.imported.processing", ConnectionStringSetting = "RabbitMQ")] byte[] data, ExecutionContext context)
        {
            BaseModel<ImportFileMessage> request = data.Deserialize<BaseModel<ImportFileMessage>>();
            var activityId = Guid.NewGuid();
            var eventMessage = request.Message;
            _logger.LogInformation($"CorrelationId: {request.Message.CorrelationId} | Starting FileImportProcessing - RunAsync");

            try
            {
                // setup Domain to use inside repository
                _tenantContext.SetTenantId(eventMessage.TenantId);
                _tenantContext.SetSubscriptionId(eventMessage.SubscriptionId);
                _tenantContext.SetProjectId(eventMessage.ProjectId);

                _userContext.SetUpn(eventMessage.RequestedBy);

                var result = await _importService.ImportFileAsync(eventMessage.RequestedBy, activityId, context, eventMessage.ObjectType, eventMessage.FileNames, eventMessage.DateTimeFormat, eventMessage.DateTimeOffset, eventMessage.CorrelationId);
                await LogActivityAsync(result, eventMessage.RequestedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CorrelationId: {request.Message.CorrelationId} | Error in FileImportProcessing - RunAsync");
                throw;
            }
        }

        private Task LogActivityAsync(ImportExportBasePayload message, string requestedBy)
        {
            var activityMessage = message.CreateLog(requestedBy, _tenantContext, _auditLogService.AppLevel);
            return _auditLogService.SendLogAsync(activityMessage);
        }
    }
}
