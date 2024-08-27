using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Function.FileParser.Constant;
using AHI.Device.Function.FileParser.Abstraction;
using System.Linq;
using Function.Extension;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Audit.Model;
using AHI.Device.Function.FileParser.ErrorTracking.Model;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Device.Function.Constant;
using AHI.Infrastructure.Exception;
using System.Text;
using Newtonsoft.Json;

namespace AHI.Device.Function.Service
{
    public class FileImportService : IFileImportService
    {
        private readonly IImportNotificationService _notification;
        private readonly IParserContext _context;
        private readonly IDictionary<Type, Infrastructure.Import.Abstraction.IFileImport> _importHandlers;
        private readonly IDictionary<string, IImportTrackingService> _errorHandlers;
        private readonly IStorageService _storageService;
        private readonly ILoggerAdapter<FileImportService> _logger;
        private IImportTrackingService _errorService;

        public FileImportService(IImportNotificationService notification,
                                 IDictionary<string, IImportTrackingService> errorHandlers,
                                 IDictionary<Type, Infrastructure.Import.Abstraction.IFileImport> importHandlers,
                                 IParserContext context,
                                 IStorageService storageService,
                                 ILoggerAdapter<FileImportService> logger)
        {
            _notification = notification;
            _errorHandlers = errorHandlers;
            _context = context;
            _importHandlers = importHandlers;
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<ImportExportBasePayload> ImportFileAsync(string upn, Guid activityId, ExecutionContext context, string objectType, IEnumerable<string> fileNames, string dateTimeFormat, string dateTimeOffset, Guid correlationId)
        {
            _logger.LogInformation($"CorrelationId: {correlationId} | Starting FileImportService - ImportFileAsync");

            try
            {
                var mimeType = Constant.EntityFileMapping.GetMimeType(objectType);
                var modelType = Constant.EntityFileMapping.GetEntityType(objectType);
                _errorService = _errorHandlers[mimeType];
                _context.SetContextFormat(ContextFormatKey.DATETIMEFORMAT, dateTimeFormat);
                _context.SetContextFormat(ContextFormatKey.DATETIMEOFFSET, DateTimeExtensions.ToValidOffset(dateTimeOffset));
                _context.SetExecutionContext(context, ParseAction.IMPORT);

                _notification.Upn = upn;
                _notification.ActivityId = activityId;
                _notification.ObjectType = objectType;
                _notification.NotificationType = ActionType.Import;

                // remove token and duplicate files
                var files = PreProcessFileNames(fileNames);

                // send signalR starting import
                _logger.LogInformation($"CorrelationId: {correlationId} | Starting FileImportService - SendStartNotifyAsync");
                await _notification.SendStartNotifyAsync(files.Count());

                foreach (string file in files)
                {
                    _errorService.File = file;
                    using (var stream = new System.IO.MemoryStream())
                    {
                        _logger.LogInformation($"CorrelationId: {correlationId} | Starting FileImportService - DownloadImportFileAsync");
                        await DownloadImportFileAsync(file, stream, correlationId);

                        if (stream.CanRead)
                        {
                            try
                            {
                                var fileImport = _importHandlers[modelType];
                                _logger.LogTrace($"CorrelationId: {correlationId} | FileImportService CommitAsync: {file} {modelType.ToJson}");
                                _logger.LogInformation($"CorrelationId: {correlationId} | Starting FileImportService - ImportAsync");

                                if (objectType == IOEntityType.DEVICE || objectType == IOEntityType.DEVICE_TEMPLATE)
                                {
                                    await fileImport.ImportAsync(stream, correlationId);
                                }
                                else
                                {
                                    await fileImport.ImportAsync(stream);
                                }
                            }
                            catch (EntityValidationException entityValidationException)
                            {
                                var errorMessage = new StringBuilder();
                                errorMessage.AppendLine(entityValidationException.Message);
                                foreach (var f in entityValidationException.Failures)
                                {
                                    errorMessage.AppendLine($"- {string.Join(", ", f.Value)}");
                                }

                                _errorService.RegisterError(errorMessage.ToString(), ErrorType.VALIDATING);
                                _logger.LogError(entityValidationException, $"CorrelationId: {correlationId} | Error in FileImportService - ImportFileAsync {entityValidationException.Message}");
                            }
                            catch (Exception ex)
                            {
                                _errorService.RegisterError(ex.Message, ErrorType.UNDEFINED);
                                _logger.LogError(ex, $"CorrelationId: {correlationId} | Error in FileImportService - ImportFileAsync {ex.Message}");
                            }
                        }
                    }
                }

                var status = GetFinishStatus(out var partialInfo);

                _logger.LogInformation($"CorrelationId: {correlationId} | Starting FileImportService - SendFinishImportNotifyAsync");
                var payload = await _notification.SendFinishImportNotifyAsync(status, partialInfo);

                return CreateLogPayload(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CorrelationId: {correlationId} | Error in FileImportService - ImportFileAsync {ex.Message}");
                throw;
            }
        }

        private async Task DownloadImportFileAsync(string filePath, System.IO.Stream outputStream, Guid correlationId)
        {
            try
            {
                await _storageService.DownloadFileToStreamAsync(filePath, outputStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CorrelationId: {correlationId} | Error in FileImportService - DownloadImportFileAsync {ex.Message}");
                await outputStream.DisposeAsync();
                _errorService.RegisterError(ImportErrorMessage.IMPORT_ERROR_GET_FILE_FAILED, Constant.ErrorType.UNDEFINED);
            }
        }

        private IEnumerable<string> PreProcessFileNames(IEnumerable<string> fileNames)
        {
            return fileNames.Select(StringExtension.RemoveFileToken).Distinct();
        }


        private ActionStatus GetFinishStatus(out (int, int) partialInfo)
        {
            var total = _errorService.FileErrors.Keys.Count;
            var failCount = _errorService.FileErrors.Where(x => x.Value.Count > 0).Count();
            if (failCount == 0)
            {
                partialInfo = (total, 0);
                return ActionStatus.Success;
            }

            if (failCount == total)
            {
                partialInfo = (0, total);
                return ActionStatus.Fail;
            }

            var successCount = total - failCount;
            partialInfo = (successCount, failCount);
            return ActionStatus.Partial;
        }

        private ImportExportBasePayload CreateLogPayload(ImportExportNotifyPayload payload)
        {
            return new ImportExportLogPayload<TrackError>(payload)
            {
                Detail = _errorService.FileErrors.Select(x => new ImportPayload<TrackError>(x.Key, x.Value)).ToArray()
            };
        }
    }
}
