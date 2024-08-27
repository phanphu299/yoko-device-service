using System.Threading.Tasks;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.Audit.Model;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Microsoft.Azure.WebJobs;

namespace AHI.Device.Function.Trigger.RabbitMQ
{
    public class FileExportProcessing
    {
        private readonly ITenantContext _tenantContext;
        private readonly IFileExportService _fileExportService;
        private readonly IAuditLogService _auditLogService;
        public FileExportProcessing(ITenantContext tenantContext, IFileExportService fileExportService, IAuditLogService auditLogService)
        {
            _tenantContext = tenantContext;
            _fileExportService = fileExportService;
            _auditLogService = auditLogService;
        }

        [FunctionName("FileExportProcessing")]
        public async Task RunAsync(
        [RabbitMQTrigger("device.function.file.exported.processing", ConnectionStringSetting = "RabbitMQ")] byte[] data, ExecutionContext context)
        {
            BaseModel<ExportFileMessage> request = data.Deserialize<BaseModel<ExportFileMessage>>();
            var eventMessage = request.Message;

            // setup Domain to use inside repository
            _tenantContext.SetTenantId(eventMessage.TenantId);
            _tenantContext.SetSubscriptionId(eventMessage.SubscriptionId);
            _tenantContext.SetProjectId(eventMessage.ProjectId);

            var result = await _fileExportService.ExportFileAsync(eventMessage.RequestedBy, eventMessage.ActivityId, context, eventMessage.ObjectType, eventMessage.Ids, eventMessage.DateTimeFormat, eventMessage.DateTimeOffset);
            await LogActivityAsync(result, eventMessage.RequestedBy);
        }
        private Task LogActivityAsync(ImportExportBasePayload message, string requestedBy)
        {
            var activityMessage = message.CreateLog(requestedBy, _tenantContext, _auditLogService.AppLevel);
            return _auditLogService.SendLogAsync(activityMessage);
        }
    }
}
