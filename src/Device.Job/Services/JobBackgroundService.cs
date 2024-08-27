using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.Exception.Helper;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Device.Application.Extension;
using Device.Job.Constant;
using Device.Job.Model;
using Device.Job.Service.Abstraction;
using Microsoft.Extensions.Configuration;
using AHI.Infrastructure.Audit.Constant;
using Device.Job.Event;
using AHI.Infrastructure.UserContext.Abstraction;
using AHI.Infrastructure.Audit.Service.Abstraction;
using System.Collections.Generic;
using System.Runtime;

namespace Device.Job.Service
{
    public class JobBackgroundService : BackgroundService
    {
        private readonly ICache _cache;

        // Used to create a thread-safe data pipeline.
        // Channels provide a thread-safe way to handle data between producers and consumers.
        private readonly Channel<QueueMessage> _channel;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILoggerAdapter<JobBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        public static readonly string WORKING_DIRECTORY = "tmp";
        private Timer _timer;
        private static int _threadIndex = 0;

        public JobBackgroundService(IServiceProvider serviceProvider,
                                        ICache cache,
                                        IConfiguration configuration,
                                        ILoggerAdapter<JobBackgroundService> logger)
        {
            _channel = Channel.CreateUnbounded<QueueMessage>();
            _cache = cache;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                int numberOfConsumerTasks = string.IsNullOrEmpty(_configuration["ApplicationSetings:ConsumerTasks"].ToString()) ? 5 : int.Parse(_configuration["ApplicationSetings:ConsumerTasks"].ToString());
                int collectGarbageInMinutes = string.IsNullOrEmpty(_configuration["ApplicationSetings:CollectGarbageInMinutes"].ToString()) ? 15 : int.Parse(_configuration["ApplicationSetings:CollectGarbageInMinutes"].ToString());

                if (Directory.Exists(WORKING_DIRECTORY))
                {
                    // delete the working directory including the temp file
                    Directory.Delete(WORKING_DIRECTORY, true);
                }

                Directory.CreateDirectory(WORKING_DIRECTORY);

                _timer = new Timer(CollectGarbage, null, TimeSpan.Zero, TimeSpan.FromMinutes(collectGarbageInMinutes));

                // This approach ensures efficient and scalable data processing in a background service using multiple threads
                var consumerTasks = new List<Task>();

                for (int i = 0; i < numberOfConsumerTasks; i++) // Create 5 consumer tasks
                {
                    // The Interlocked.Increment(ref _threadIndex) method is used to safely increment the thread index.
                    int currentIndex = Interlocked.Increment(ref _threadIndex);

                    // Each consumer task runs the ConsumeDataAsync method, which reads messages from the channel until the channel is empty and the writer is completed.
                    // The writer is only completed when calling _channel.Writer.Complete
                    consumerTasks.Add(Task.Run(() => ConsumeDataAsync(stoppingToken, currentIndex), stoppingToken));
                }

                // await Task.WhenAll(consumerTasks) will end when all the consumer tasks have processed all the messages and the channel is empty and completed.
                await Task.WhenAll(consumerTasks); // Wait for all consumers to complete
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in JobBackgroundService - ExecuteAsync {ex.Message}");
            }
        }

        private async Task ConsumeDataAsync(CancellationToken stoppingToken, int threadIndex)
        {
            _logger.LogInformation($"ThreadIndex: {threadIndex} | Start ConsumeDataAsync");
            ITenantContext tenantContext = null;
            JobInfo jobInfo = null;

            // This method asynchronously waits until data is available in the channel, avoiding busy-waiting and high CPU usage.
            // Using await with WaitToReadAsync ensures that your consumer tasks only use CPU resources when there is data to process, 
            // making your application more efficient and responsive.
            while (await _channel.Reader.WaitToReadAsync(stoppingToken))
            {
                // This method attempts to read data from the channel if available.
                if (_channel.Reader.TryRead(out var queueMessage))
                {
                    try
                    {
                        Guid activityId = queueMessage.JobInfo.Payload.ContainsKey(NotificationKeys.ACTIVITY_ID) ? Guid.Parse(queueMessage.JobInfo.Payload[NotificationKeys.ACTIVITY_ID].ToString()) : Guid.NewGuid();
                        Guid widgetId = queueMessage.JobInfo.Payload.ContainsKey(NotificationKeys.WIDGET_ID) ? Guid.Parse(queueMessage.JobInfo.Payload[NotificationKeys.WIDGET_ID].ToString()) : Guid.NewGuid();
                        var start = DateTime.UtcNow;

                        if (queueMessage.JobInfo.Payload.ContainsKey(NotificationKeys.ENTITY_NAME) &&
                            queueMessage.JobInfo.Payload[NotificationKeys.ENTITY_NAME].ToString().ToLower().StartsWith("dev-debug-"))
                        {
                            _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | ThreadIndex: {threadIndex} | Process ConsumeDataAsync: ${queueMessage.ToJson()}");
                        }
                        else
                        {
                            _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | ThreadIndex: {threadIndex} | Process ConsumeDataAsync");
                        }

                        tenantContext = queueMessage.TenantContext;
                        jobInfo = queueMessage.JobInfo;

                        switch (jobInfo.Type)
                        {
                            case JobType.QUERY_TIMESERIES:
                                await ProcessQueryTimeSeriesAsync(tenantContext, jobInfo, activityId, widgetId);
                                break;
                            case JobType.QUERY_TIMESERIES_FULL_DATA:
                                await ProcessQueryTimeSeriesFullDataAsync(tenantContext, jobInfo, activityId, widgetId);
                                break;
                        }

                        _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | ThreadIndex: {threadIndex} | End Process ConsumeDataAsync after {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"ThreadIndex: {threadIndex} | Error in JobBackgroundService - ExecuteAsync - ProcessQuery... {ex.Message}");

                        await UpdateJobAsync(tenantContext, jobInfo.Id, string.Empty, ex.Message);
                        if (jobInfo.Payload.ContainsKey(NotificationKeys.SEND_NOTIFICATION) &&
                            bool.Parse(jobInfo.Payload[NotificationKeys.SEND_NOTIFICATION]?.ToString() ?? "false"))
                        {
                            await SendNotificationAndAuditAsync(tenantContext, jobInfo, ActionStatus.Fail);
                        }
                    }
                }
            }

            _logger.LogInformation($"ThreadIndex: {threadIndex} | End ConsumeDataAsync");
        }

        private async Task SendNotificationAndAuditAsync(
            ITenantContext tenantContext,
            JobInfo jobInfo,
            ActionStatus actionStatus)
        {
            using (var scope = _serviceProvider.CreateNewScope(tenantContext))
            {
                var exportNotificationService = scope.ServiceProvider.GetRequiredService<IExportNotificationService>();
                var auditLogService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
                var userContext = scope.ServiceProvider.GetRequiredService<IUserContext>();

                exportNotificationService.ActivityId = Guid.Parse(jobInfo.Payload[NotificationKeys.ACTIVITY_ID].ToString());
                exportNotificationService.NotificationType = (ActionType)Enum.Parse(typeof(ActionType), jobInfo.Payload[NotificationKeys.NOTIFICATION_TYPE].ToString());
                exportNotificationService.ObjectType = jobInfo.Payload[NotificationKeys.ENTITY_TYPE].ToString();
                exportNotificationService.Upn = jobInfo.Payload[NotificationKeys.UPN].ToString();

                if (actionStatus == ActionStatus.Success)
                {
                    var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
                    var exportUrl = await storageService.UploadFromEndpointAsync(jobInfo.Payload[NotificationKeys.FILE_PATH].ToString(), jobInfo.Payload[NotificationKeys.FILE_NAME].ToString(), $"{_configuration["Api:PublicEndpoint"]}/dev/jobs/{jobInfo.Id}.{jobInfo.OutputType}");
                    exportNotificationService.URL = exportUrl;
                }

                await exportNotificationService.SendFinishExportNotifyAsync(actionStatus, jobInfo.Payload[NotificationKeys.APPLICATION_ID].ToString(), exportNotificationService.NotificationType == ActionType.Export);
                var activityLogEvent = (ActivitiesLogEvent)Enum.Parse(typeof(ActivitiesLogEvent), jobInfo.Payload[NotificationKeys.ACTIVITY_LOG_EVENT].ToString());
                userContext.SetUpn(jobInfo.Payload[NotificationKeys.UPN].ToString());
                await auditLogService.SendLogAsync(jobInfo.Payload[NotificationKeys.ENTITY_TYPE].ToString(), activityLogEvent, actionStatus, jobInfo.Payload[NotificationKeys.ENTITY_ID].ToString(), jobInfo.Payload[NotificationKeys.ENTITY_NAME].ToString(), jobInfo.Payload);
            }
        }

        private async Task ProcessQueryTimeSeriesAsync(ITenantContext tenantContext, JobInfo jobInfo, Guid activityId, Guid widgetId)
        {
            try
            {
                _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | Start ProcessQueryTimeSeriesAsync");

                var header = false;
                using (var scope = _serviceProvider.CreateNewScope(tenantContext))
                {
                    var timeseriesService = scope.ServiceProvider.GetRequiredService<IDataSourceService>();
                    var csvOutputFileService = scope.ServiceProvider.GetRequiredService<IOutputFileService>();
                    var filePath = Path.Combine(WORKING_DIRECTORY, $"{jobInfo.Id}.csv");
                    await foreach (var flattenHistoricalData in timeseriesService.GetDataAsync(tenantContext, jobInfo))
                    {
                        if (flattenHistoricalData.Any())
                        {
                            if (!header)
                            {
                                header = true;
                                var headerString = csvOutputFileService.GetHeader(flattenHistoricalData);
                                await File.WriteAllTextAsync(filePath, headerString, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                            }
                            var data = csvOutputFileService.GetData(flattenHistoricalData);
                            await File.AppendAllLinesAsync(filePath, data, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                        }
                    }
                    await UpdateJobAsync(tenantContext, jobInfo.Id, filePath);
                }

                _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | End ProcessQueryTimeSeriesAsync");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CorrelationId: {activityId} | widgetId: {widgetId} | Error in ProcessQueryTimeSeriesAsync - QueueAsync {ex.Message}");
                throw;
            }
        }

        private async Task ProcessQueryTimeSeriesFullDataAsync(ITenantContext tenantContext, JobInfo jobInfo, Guid activityId, Guid widgetId)
        {
            try
            {
                _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | Start ProcessQueryTimeSeriesFullDataAsync");

                using (var scope = _serviceProvider.CreateNewScope(tenantContext))
                {
                    var timeseriesService = scope.ServiceProvider.GetRequiredService<IDataSourceService>();
                    var csvOutputFileService = scope.ServiceProvider.GetRequiredService<IOutputFileService>();
                    var filePath = Path.Combine(WORKING_DIRECTORY, $"{jobInfo.Id}.csv");
                    var assetTimeseries = jobInfo.Payload.ToJson().FromJson<GetFullAssetAttributeSeries>();

                    if (assetTimeseries == null)
                    {
                        throw ValidationExceptionHelper.GenerateRequiredValidation(nameof(jobInfo.Payload));
                    }

                    _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | ProcessQueryTimeSeriesFullDataAsync - Start GetHeader - File.WriteAllTextAsync");
                    var headerString = csvOutputFileService.GetHeader(assetTimeseries.ColumnMappings);
                    await File.WriteAllTextAsync(filePath, headerString, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                    if (assetTimeseries.Assets != null && assetTimeseries.Assets.Any())
                    {
                        _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | ProcessQueryTimeSeriesFullDataAsync - Start timeseriesService.GetPaginationDataAsync");
                        var start = DateTime.UtcNow;
                        int index = 0;

                        await foreach (var flattenHistoricalData in timeseriesService.GetPaginationDataAsync(assetTimeseries, activityId, widgetId))
                        {
                            _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | ProcessQueryTimeSeriesFullDataAsync - End timeseriesService.GetPaginationDataAsync by Index: {index} after {DateTime.UtcNow.Subtract(start).TotalMilliseconds} at {DateTime.UtcNow}");
                            start = DateTime.UtcNow;

                            if (flattenHistoricalData.Any())
                            {
                                List<string> data = csvOutputFileService.GetData(flattenHistoricalData, assetTimeseries.ColumnMappings, assetTimeseries.TimezoneOffset, assetTimeseries.DateTimeFormat, activityId, widgetId);

                                _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | ProcessQueryTimeSeriesFullDataAsync - End csvOutputFileService.GetData by Index: {index} after {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
                                start = DateTime.UtcNow;

                                await File.AppendAllLinesAsync(filePath, data, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

                                _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | ProcessQueryTimeSeriesFullDataAsync - End File.AppendAllLinesAsync by Index: {index}, filePath: {filePath}, jobId: {jobInfo.Id} after {DateTime.UtcNow.Subtract(start).TotalMilliseconds}, Total Record: {data.Count} at {DateTime.UtcNow}");
                            }

                            index++;
                        }

                        _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | ProcessQueryTimeSeriesFullDataAsync - End timeseriesService.GetPaginationDataAsync");
                    }

                    _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | ProcessQueryTimeSeriesFullDataAsync - Start UpdateJobAsync");
                    await UpdateJobAsync(tenantContext, jobInfo.Id, filePath);

                    if (jobInfo.Payload.ContainsKey(NotificationKeys.SEND_NOTIFICATION) &&
                        bool.Parse(jobInfo.Payload[NotificationKeys.SEND_NOTIFICATION]?.ToString() ?? "false"))
                    {
                        _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | ProcessQueryTimeSeriesFullDataAsync - Start SendNotificationAndAuditAsync");
                        await SendNotificationAndAuditAsync(tenantContext, jobInfo, ActionStatus.Success);
                    }
                }

                _logger.LogInformation($"CorrelationId: {activityId} | widgetId: {widgetId} | End ProcessQueryTimeSeriesFullDataAsync");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CorrelationId: {activityId} | widgetId: {widgetId} | Error in ProcessQueryTimeSeriesFullDataAsync - QueueAsync {ex.Message}");
                throw;
            }
        }

        public async Task QueueAsync(ITenantContext tenantContext, JobInfo jobInfo, Guid correlationId)
        {
            try
            {
                await _channel.Writer.WriteAsync(new QueueMessage(tenantContext, jobInfo));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"CorrelationId: {correlationId} | Error in JobBackgroundService - QueueAsync {ex.Message}");
                throw;
            }
        }

        private async Task UpdateJobAsync(ITenantContext tenantContext, Guid id, string filePath, string failedMessage = null)
        {
            try
            {
                var key = JobService.GetJobKey(tenantContext, id);
                var jobInfo = await _cache.GetAsync<JobInfo>(key);

                if (jobInfo == null)
                    return;

                jobInfo.FilePath = filePath;
                jobInfo.FailedMessage = failedMessage;
                jobInfo.Status = failedMessage == null ? JobStatus.COMPLETED : JobStatus.FAILED;

                await _cache.StoreAsync(key, jobInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in JobBackgroundService - UpdateJobAsync {ex.Message}");
            }
        }

        private void CollectGarbage(object state)
        {
            _logger.LogInformation("JobBackgroundService - Forcing garbage collection.");
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
