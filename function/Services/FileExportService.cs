using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using AHI.Device.Function.Service.Abstraction;
using Microsoft.Azure.WebJobs;
using System.Linq;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Device.Function.FileParser.Constant;
using Function.Extension;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.Audit.Model;
using AHI.Infrastructure.Audit.Constant;
using AHI.Device.Function.FileParser.ErrorTracking.Model;
using ValidationMessage = AHI.Device.Function.Constant.ErrorMessage.FluentValidation;

namespace AHI.Device.Function.Service
{
    public class FileExportService : IFileExportService
    {
        private readonly IExportNotificationService _notification;
        private readonly IExportTrackingService _errorService;
        private readonly IParserContext _context;
        private readonly IDictionary<string, IExportHandler> _exportHandler;
        private readonly ILoggerAdapter<FileExportService> _logger;

        public FileExportService(IExportNotificationService notificationService, IExportTrackingService errorService,
                                 IParserContext parserContext, IDictionary<string, IExportHandler> exportHandler,
                                 ILoggerAdapter<FileExportService> logger)
        {
            _notification = notificationService;
            _errorService = errorService;
            _context = parserContext;
            _exportHandler = exportHandler;
            _logger = logger;
        }

        public async Task<ImportExportBasePayload> ExportFileAsync(string upn, Guid activityId, ExecutionContext context, string objectType, IEnumerable<string> ids,
                                                            string dateTimeFormat, string dateTimeOffset)
        {
            _context.SetContextFormat(ContextFormatKey.DATETIMEFORMAT, dateTimeFormat);
            _context.SetContextFormat(ContextFormatKey.DATETIMEOFFSET, DateTimeExtensions.ToValidOffset(dateTimeOffset));

            _notification.Upn = upn;
            _notification.ActivityId = activityId;
            _notification.ObjectType = objectType;
            _notification.NotificationType = ActionType.Export;

            await _notification.SendStartNotifyAsync(ids.Count());
            try
            {
                if (_exportHandler.TryGetValue(objectType, out var handler))
                {
                    var downloadUrl = await handler.HandleAsync(context.FunctionAppDirectory, ids);
                    if (!string.IsNullOrEmpty(downloadUrl))
                    {
                        _notification.URL = downloadUrl;
                        _logger.LogInformation($"Download url: {downloadUrl}");
                    }
                }
                else
                    _errorService.RegisterError(ValidationMessage.EXPORT_NOT_SUPPORTED);
            }
            catch (Exception e)
            {
                _errorService.RegisterError(e.Message);
                _logger.LogError(e, e.Message);
            }
            var status = GetFinishStatus();
            var payload = await _notification.SendFinishExportNotifyAsync(status);
            return CreateLogPayload(payload);
        }

        private ActionStatus GetFinishStatus()
        {
            return _errorService.HasError ? ActionStatus.Fail : ActionStatus.Success;
        }

        private ImportExportLogPayload<TrackError> CreateLogPayload(ImportExportNotifyPayload payload)
        {
            var detail = new[] { new ExportPayload<TrackError>((payload as ExportNotifyPayload).URL, _errorService.GetErrors) };
            return new ImportExportLogPayload<TrackError>(payload)
            {
                Detail = detail
            };
        }

    }
}
