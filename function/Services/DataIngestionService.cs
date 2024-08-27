using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Device.Function.Model;
using System.IO;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Model.Notification;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Function.Extension;
using AHI.Infrastructure.SharedKernel.Model;
using Function.Exception;
using AHI.Device.Function.FileParser.Abstraction;
using AHI.Infrastructure.Interceptor.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Device.Function.Events;
using AHI.Infrastructure.Audit.Service.Abstraction;
using AHI.Infrastructure.Audit.Model;
using AHI.Infrastructure.Audit.Constant;
using AHI.Infrastructure.Repository.Abstraction.ReadOnly;
using AHI.Infrastructure.Repository.Abstraction;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using AHI.Device.Function.Service.Model;
using Npgsql;
using AHI.Infrastructure.Exception;
using CsvHelper;

namespace AHI.Device.Function.Service
{
    public class DataIngestionService : IDataIngestionService
    {
        private readonly IConfiguration _configuration;
        private readonly IStorageService _storageService;
        private readonly IRuntimeAttributeService _runtimeAttributeService;
        private readonly INotificationService _notificationService;
        private readonly IAssetNotificationService _notificationAssetService;
        private readonly IDomainEventDispatcher _domainEventDispatcher;
        private readonly IReadOnlyDeviceRepository _readOnlyDeviceRepository;
        private readonly ITenantContext _tenantContext;
        private readonly IFileIngestionTrackingService _errorService;
        private readonly IDynamicResolver _dynamicResolver;
        private readonly ILoggerAdapter<DataIngestionService> _logger;
        private readonly IDeviceHeartbeatService _deviceHeartbeatService;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IReadOnlyDbConnectionFactory _readOnlyDbConnectionFactory;

        private readonly Guid ActivityId;
        private Stopwatch _stopwatch;

        private readonly string[] SERIES_DATA_TYPES = new[] { DataTypeConstants.TYPE_DOUBLE, DataTypeConstants.TYPE_INTEGER, DataTypeConstants.TYPE_BOOLEAN, DataTypeConstants.TYPE_TEXT };
        private const string NOTIFY_ENDPOINT = "ntf/notifications/ingest/notify";
        private const string ENTITY_NAME = "File Ingestion";
        private const string INITIATED = "System";
        private const string CSV_FORMAT__KEY_HEADER = "DeviceId";
        private const int CSV_FORMAT__KEY_HEADER_INDEX = 0;
        private const int CSV_FORMAT__KEY_VALUE_INDEX = 1;

        private readonly BatchProcessingOptions _batchOptions;

        public DataIngestionService(
            IConfiguration configuration,
            IDbConnectionFactory dbConnectionFactory,
            IDeviceRepository deviceRepository,
            IStorageService storageService,
            IRuntimeAttributeService runtimeAttributeService,
            INotificationService notificationService,
            IAssetNotificationService notificationAssetService,
            IDomainEventDispatcher domainEventDispatcher,
            IReadOnlyDeviceRepository readOnlyDeviceRepository,
            ITenantContext tenantContext,
            IFileIngestionTrackingService errorService,
            IDynamicResolver dynamicResolver,
            IDeviceHeartbeatService deviceHeartbeatService,
            ILoggerAdapter<DataIngestionService> logger,
            IAuditLogService auditLogService,
            IReadOnlyDbConnectionFactory readOnlyDbConnectionFactory)
        {
            _configuration = configuration;
            _storageService = storageService;
            _runtimeAttributeService = runtimeAttributeService;
            _notificationService = notificationService;
            _notificationAssetService = notificationAssetService;
            _domainEventDispatcher = domainEventDispatcher;
            _tenantContext = tenantContext;
            _errorService = errorService;
            _dynamicResolver = dynamicResolver;
            _logger = logger;
            ActivityId = Guid.NewGuid();
            _deviceHeartbeatService = deviceHeartbeatService;
            _readOnlyDeviceRepository = readOnlyDeviceRepository;
            _dbConnectionFactory = dbConnectionFactory;
            _deviceRepository = deviceRepository;
            _auditLogService = auditLogService;
            _readOnlyDbConnectionFactory = readOnlyDbConnectionFactory;

            _stopwatch = Stopwatch.StartNew();

            var batchOptions = _configuration.GetSection("BatchProcessing").Get<BatchProcessingOptions>();
            if (batchOptions is null)
                _batchOptions = new BatchProcessingOptions() { InsertSize = 25000 };
            else
                _batchOptions = batchOptions;
        }

        public async Task IngestDataAsync(DataIngestionMessage eventMessage)
        {
            WriteLog(LogLevel.Information, $"[IngestDataAsync][{ActivityId}] Starting...");
            WriteLog(LogLevel.Trace, $"[IngestDataAsync][{ActivityId}] Message details: {eventMessage.ToJson()}...");
            _stopwatch = Stopwatch.StartNew();
            var processStatus = new ProcessStatus();
            using (var stream = new MemoryStream())
            {
                await DownloadIngestFileAsync(eventMessage.FilePath, stream);
                WriteLogWithStopwatch(LogLevel.Trace, $"[IngestDataAsync][{ActivityId}] Downloaded Ingest data from File...");
                if (stream.CanRead)
                {
                    stream.Position = 0;
                    using (var reader = new StreamReader(stream))
                    {
                        using (var csvReader = CsvHelperExtension.CreateCsvHelper(reader))
                        {
                            var (ingestionInfo, rowIndex) = await ProcessMetadataAsync(csvReader, processStatus);
                            WriteLogWithStopwatch(LogLevel.Debug, $"[IngestDataAsync][{ActivityId}] Processed Metadata: {ingestionInfo.DeviceIdFromFile}...");
                            WriteLog(LogLevel.Trace, $"[IngestDataAsync][{ActivityId}] Metadata details: {ingestionInfo.ToJson()}...");

                            // tracking the device heart beat
                            await _deviceHeartbeatService.TrackingHeartbeatAsync(_tenantContext.ProjectId, ingestionInfo.DeviceIdFromFile);
                            WriteLogWithStopwatch(LogLevel.Trace, $"[IngestDataAsync][{ActivityId}] Tracked Heartbeat...");
                            using (var dbConnection = _dbConnectionFactory.CreateConnection())
                            {
                                await dbConnection.OpenAsync();
                                WriteLog(LogLevel.Trace, $"[IngestDataAsync][{ActivityId}] Connection Opened...");
                                using (var transaction = dbConnection.BeginTransaction())
                                {
                                    WriteLogWithStopwatch(LogLevel.Trace, $"[IngestDataAsync][{ActivityId}] Started Transaction...");
                                    // Loop for get device metrics
                                    var (metricNumerics, metricTexts) = await ProcessHistoricalDataAsync(csvReader, rowIndex, ingestionInfo, dbConnection, processStatus);
                                    WriteLogWithStopwatch(LogLevel.Trace, $"[IngestDataAsync][{ActivityId}] Processed Data...");

                                    await InsertSnapshotAsync(metricNumerics, metricTexts, dbConnection);
                                    WriteLogWithStopwatch(LogLevel.Trace, $"[IngestDataAsync][{ActivityId}] Inserted snapshots...");

                                    try
                                    {
                                        await transaction.CommitAsync();
                                        WriteLogWithStopwatch(LogLevel.Debug, $"[IngestDataAsync][{ActivityId}] Committed historical data...");
                                    }
                                    catch (Exception e)
                                    {
                                        await transaction.RollbackAsync();
                                        _errorService.RegisterError(e.Message, ErrorType.DATABASE);
                                        await LogActivityAsync(eventMessage.FilePath, ActionStatus.Fail);
                                        WriteLogWithStopwatch(LogLevel.Error, $"[IngestDataAsync][{ActivityId}] Commit Failed...", e);
                                        throw;
                                    }

                                    // Changed to Write Repository, as we just completed the Bulk process with million data, so we can not trust the Read Only DB in this state.
                                    var deviceMetricsSnapshot = await _deviceRepository.GetMetricDataTypesAsync(ingestionInfo.DeviceIdFromFile);
                                    WriteLogWithStopwatch(LogLevel.Trace, $"[IngestDataAsync][{ActivityId}] Query snapshot data...");

                                    var valueMetricsSnapshot = deviceMetricsSnapshot.ToDictionary(x => x.MetricKey, y => ParseValue(y.DataType, y.Value));
                                    WriteLogWithStopwatch(LogLevel.Trace, $"[IngestDataAsync][{ActivityId}] Parsed snapshot data by data type...");

                                    CalculateMetricValue(deviceMetricsSnapshot, valueMetricsSnapshot);
                                    WriteLogWithStopwatch(LogLevel.Trace, $"[IngestDataAsync][{ActivityId}] Calculated aggregation metric data by data type...");

                                    var aggregationMetrics = deviceMetricsSnapshot.Where(x => x.MetricType == TemplateKeyTypes.AGGREGATION).Select(x => new MetricSnapshotDto()
                                    {
                                        DeviceId = ingestionInfo.DeviceIdFromFile,
                                        MetricKey = x.MetricKey,
                                        Value = valueMetricsSnapshot[x.MetricKey],
                                        Timestamp = DateTime.UtcNow // TODO: The Timestamp here might cause some inconsistent issue - need to check again
                                    });

                                    if (aggregationMetrics.Any())
                                    {
                                        await ExecuteInsertSnapshotAsync(dbConnection, aggregationMetrics);
                                        WriteLogWithStopwatch(LogLevel.Trace, $"[IngestDataAsync][{ActivityId}] Stored aggregation metrics data...");
                                    }

                                    await dbConnection.CloseAsync();

                                    var runtimeAssetIds = await _deviceRepository.GetAssetTriggerAsync(_tenantContext.ProjectId, ingestionInfo.DeviceIdFromFile);
                                    var relevantAssetIds = await _deviceRepository.GetAssetIdsAsync(_tenantContext.ProjectId, ingestionInfo.DeviceIdFromFile);
                                    var timestamp = metricNumerics.Select(x => x.Timestamp).Union(metricTexts.Select(x => x.Timestamp)).OrderByDescending(x => x).First();
                                    var runtimeAffectedAssetIds = await _runtimeAttributeService.CalculateRuntimeValueAsync(_tenantContext.ProjectId, timestamp, runtimeAssetIds);
                                    var tasks = new List<Task>();
                                    foreach (var assetId in relevantAssetIds.Union(runtimeAffectedAssetIds).Distinct())
                                    {
                                        tasks.Add(_notificationAssetService.NotifyAssetAsync(new AssetNotificationMessage(NotificationType.ASSET, assetId)));
                                        var unixTimeStamp = timestamp.ToUtcDateTimeOffset().ToUnixTimeMilliseconds();
                                        tasks.Add(_domainEventDispatcher.SendAsync(new AssetAttributeChangedEvent(assetId, unixTimeStamp, _tenantContext)));
                                    }
                                    await Task.WhenAll(tasks);
                                    WriteLogWithStopwatch(LogLevel.Trace, $"[IngestDataAsync][{ActivityId}] Others - Calculate affected Asset Attributes...");
                                }
                            }
                        }
                    }
                }
            }

            if (processStatus.IsSuccess)
            {
                await SendFileIngestionStatusNotifyAsync(ActionStatus.Success, Constant.DescriptionMessage.INGEST_SUCCESS);
                await LogActivityAsync(eventMessage.FilePath, ActionStatus.Success);
                WriteLog(LogLevel.Information, $"[IngestDataAsync][{ActivityId}] Successful...");
            }
            else
            {
                await LogActivityAsync(eventMessage.FilePath, ActionStatus.Fail);
                WriteLog(LogLevel.Error, $"[IngestDataAsync][{ActivityId}] Failed...");
            }

            WriteLogWithStopwatch(LogLevel.Information, $"[IngestDataAsync][{ActivityId}] Completed!");

        }

        private async Task<(DeviceIngestionMetadata IngestionMetadata, int RowIndex)> ProcessMetadataAsync(CsvReader csvReader, ProcessStatus processStatus)
        {
            var deviceIdFromFile = string.Empty;
            int rowIndex = 0;

            // Reading the First Line for Device's ID
            if (await csvReader.ReadAsync()
                && ValidateFirstLineCsvFile(csvReader))
            {
                rowIndex++;

                deviceIdFromFile = csvReader.GetField(CSV_FORMAT__KEY_VALUE_INDEX);
                var deviceInformation = await _readOnlyDeviceRepository.GetDeviceInformationAsync(_tenantContext.ProjectId, new[] { deviceIdFromFile });
                if (deviceInformation == null)
                {
                    processStatus.UpdateStatus(false);
                    HandleIngestionError(Constant.DescriptionMessage.DEVICE_FILE_INGESTION_DEVICEID_NOT_EXIST, rowIndex, CSV_FORMAT__KEY_HEADER);
                }
                WriteLogWithStopwatch(LogLevel.Trace, $"[ProcessMetadataAsync][{ActivityId}] Validate device id successful...");

                // Get exactly timestamp key from database
                var timestampKey = deviceInformation.Metrics.Where(x => x.MetricType == TemplateKeyTypes.TIMESTAMP).Single().MetricKey; // We wont have more than 1 timestamp key.

                // Reading the Second Line for Headers
                if (await csvReader.ReadAsync())
                {
                    rowIndex++;
                    var deviceMetricsFromFile = csvReader.Parser.Record.ToList();
                    // Get device metric names
                    WriteLogWithStopwatch(LogLevel.Trace, $"[ProcessMetadataAsync][{ActivityId}] Read fields from file successful...");

                    var timestampIndex = deviceMetricsFromFile.IndexOf(timestampKey);
                    if (timestampIndex == -1)
                    {
                        processStatus.UpdateStatus(false);
                        HandleIngestionError(Constant.DescriptionMessage.DEVICE_FILE_INGESTION_INVALID_TIMESTAMP, rowIndex, string.Join(",", timestampKey));
                    }
                    WriteLogWithStopwatch(LogLevel.Trace, $"[ProcessMetadataAsync][{ActivityId}] Validate timestamp successful...");

                    var listAvailableMetrics = await _readOnlyDeviceRepository.GetActiveDeviceMetricsAsync(_tenantContext.ProjectId, deviceIdFromFile, deviceMetricsFromFile);
                    if (!listAvailableMetrics.Any())
                    {
                        processStatus.UpdateStatus(false);
                        HandleIngestionError(Constant.DescriptionMessage.DEVICE_FILE_INGESTION_INVALID_DATA_TYPE, rowIndex);
                    }
                    WriteLogWithStopwatch(LogLevel.Trace, $"[ProcessMetadataAsync][{ActivityId}] Get listAvailableMetrics from DB & Validate metrics successful...");

                    var listActiveMetrics = listAvailableMetrics.Select(t => new MetricDataTypeDto
                    {
                        MetricKey = t.MetricKey,
                        DataType = t.DataType,
                        MetricIdx = deviceMetricsFromFile.FindIndex(x => x == t.MetricKey)
                    });
                    var metadata = new DeviceIngestionMetadata(deviceIdFromFile, timestampIndex, timestampKey, deviceInformation, deviceMetricsFromFile, listActiveMetrics);
                    WriteLogWithStopwatch(LogLevel.Trace, $"[ProcessMetadataAsync][{ActivityId}] Prepared data...");
                    return (metadata, rowIndex);
                }

                // Second line (Metrics list) not correct
                processStatus.UpdateStatus(false);
                HandleIngestionError(Constant.DescriptionMessage.DEVICE_FILE_INGESTION_INVALID_DATA_TYPE, rowIndex);
                return default;
            }

            // First line (Device Information) not correct
            processStatus.UpdateStatus(false);
            HandleIngestionError(Constant.DescriptionMessage.DEVICE_FILE_INGESTION_INVALID_FORMAT, rowIndex, CSV_FORMAT__KEY_HEADER);
            return default;
        }

        private async Task<(IEnumerable<MetricSeriesDto> numericMetrics, IEnumerable<MetricSeriesTextDto> textMetrics)> ProcessHistoricalDataAsync(
                CsvReader csvReader,
                int rowIndex,
                DeviceIngestionMetadata ingestionInfo,
                NpgsqlConnection dbConnection,
                ProcessStatus processStatus)
        {
            var metricSeriesBatch = new List<MetricSeriesDto>();
            var metricSeriesTextBatch = new List<MetricSeriesTextDto>();

            // Get active device metric values only - #16509
            var availableMetrics = ingestionInfo.ListActiveMetrics.Where(t => SERIES_DATA_TYPES.Contains(t.DataType)).ToList();

            while (csvReader.Read())
            {
                rowIndex++;
                if (csvReader.ColumnCount != ingestionInfo.DeviceMetricsFromFile.Count)
                {
                    HandleIngestionError(Constant.DescriptionMessage.DEVICE_FILE_INGESTION_INVALID_CSV_FORMAT, rowIndex);
                }
                var values = csvReader.Parser.Record.ToList();

                ProcessMetricValues(availableMetrics, values, rowIndex, ingestionInfo, metricSeriesBatch, metricSeriesTextBatch, processStatus);
                if (metricSeriesBatch.Count >= _batchOptions.InsertSize - csvReader.ColumnCount) // As `values` can contains more than 1 fields, so we dont want the Bulk Insert need to run more than 2 Rounds
                {
                    WriteLogWithStopwatch(LogLevel.Debug, $"[ProcessHistoricalDataAsync][{ActivityId}] Next batch ready for Numeric Series...");
                    await BulkInsertNumericSeriesAsync(metricSeriesBatch, dbConnection);
                    WriteLogWithStopwatch(LogLevel.Debug, $"[ProcessHistoricalDataAsync][{ActivityId}] Numeric Series inserted...");
                    metricSeriesBatch = new List<MetricSeriesDto>();
                }
                if (metricSeriesTextBatch.Count >= _batchOptions.InsertSize - csvReader.ColumnCount) // As `values` can contains more than 1 fields, so we dont want the Bulk Insert need to run more than 2 Rounds
                {
                    WriteLogWithStopwatch(LogLevel.Debug, $"[ProcessHistoricalDataAsync][{ActivityId}] Next batch ready for Text Series...");
                    await BulkInsertTextSeriesAsync(metricSeriesTextBatch, dbConnection);
                    WriteLogWithStopwatch(LogLevel.Debug, $"[ProcessHistoricalDataAsync][{ActivityId}] Text Series inserted...");
                    metricSeriesTextBatch = new List<MetricSeriesTextDto>();
                }
            }

            if (metricSeriesBatch.Count > 0)
            {
                WriteLogWithStopwatch(LogLevel.Debug, $"[ProcessHistoricalDataAsync][{ActivityId}] Last batch ready for Numeric Series...");
                await BulkInsertNumericSeriesAsync(metricSeriesBatch, dbConnection);
                WriteLogWithStopwatch(LogLevel.Debug, $"[ProcessHistoricalDataAsync][{ActivityId}] Numeric Series inserted...");
            }
            if (metricSeriesTextBatch.Count > 0)
            {
                WriteLogWithStopwatch(LogLevel.Debug, $"[ProcessHistoricalDataAsync][{ActivityId}] Last batch ready for Text Series...");
                await BulkInsertTextSeriesAsync(metricSeriesTextBatch, dbConnection);
                WriteLogWithStopwatch(LogLevel.Debug, $"[ProcessHistoricalDataAsync][{ActivityId}] Text Series inserted...");
            }

            var (numericMetrics, textMetrics) = await GetLatestMetricForDeviceAsync(dbConnection, ingestionInfo);
            WriteLogWithStopwatch(LogLevel.Trace, $"[ProcessDataAsync][{ActivityId}] Get latest series for snapshot...");

            return (numericMetrics, textMetrics);
        }

        private void ProcessMetricValues(
            IEnumerable<MetricDataTypeDto> availableMetrics,
            IList<string> values,
            int rowIndex,
            DeviceIngestionMetadata ingestionInfo,
            ICollection<MetricSeriesDto> metricSeriesBatch,
            ICollection<MetricSeriesTextDto> metricSeriesTextBatch,
            ProcessStatus processStatus)
        {
            foreach (var metric in availableMetrics.Where(metric => metric.MetricKey != ingestionInfo.TimestampKey))
            {
                var valueInput = values[metric.MetricIdx];
                if (string.IsNullOrWhiteSpace(valueInput) || string.IsNullOrEmpty(valueInput))
                    continue;

                if (DataTypeExtensions.IsNumericTypeSeries(metric.DataType))
                {
                    var dto = ProcessNumericMetricValue(metric, values, rowIndex, ingestionInfo, processStatus);
                    if (processStatus.IsSuccess)
                        metricSeriesBatch.Add(dto);

                }
                else if (DataTypeExtensions.IsTextTypeSeries(metric.DataType))
                {
                    var dto = ProcessTextMetricValue(metric, values, rowIndex, ingestionInfo, processStatus);
                    if (processStatus.IsSuccess)
                        metricSeriesTextBatch.Add(dto);
                }
                else
                {
                    // As other handler do not accept un-listed data types, should throw exception here for consistence.
                    HandleIngestionError(Constant.DescriptionMessage.DEVICE_FILE_INGESTION_INVALID_DATA_TYPE, rowIndex);
                }
            }
        }

        private async Task<(IEnumerable<MetricSeriesDto> numericMetrics, IEnumerable<MetricSeriesTextDto> textMetrics)> GetLatestMetricForDeviceAsync(IDbConnection dbConnection, DeviceIngestionMetadata ingestionInfo)
        {
            var numerics = await dbConnection.QueryAsync<MetricSeriesDto>($@"select _ts as Timestamp, device_id as DeviceId, metric_key as MetricKey, value as Value
                                                                            from(
                                                                                select _ts, device_id, metric_key, value,
                                                                                    row_number() over(partition  by metric_key order by _ts desc) as row_number
                                                                                from(
                                                                                    select _ts, device_id, metric_key, value
                                                                                    from device_metric_series
                                                                                    where device_id = @DeviceId
                                                                                    order by _ts desc
                                                                                ) tempGrp
                                                                            ) tempSeries where row_number = 1",
                                                                        new
                                                                        {
                                                                            DeviceId = ingestionInfo.DeviceIdFromFile,
                                                                        });
            WriteLogWithStopwatch(LogLevel.Trace, $"[GetLatestMetricForDeviceAsync][{ActivityId}] Query numericMetrics...");

            var texts = await dbConnection.QueryAsync<MetricSeriesTextDto>($@"select _ts as Timestamp, device_id as DeviceId, metric_key as MetricKey, value as Value
                                                                                from(
                                                                                    select _ts, device_id, metric_key, value,
                                                                                        row_number() over(partition  by metric_key order by _ts desc) as row_number
                                                                                    from(
                                                                                        select _ts, device_id, metric_key, value
                                                                                        from device_metric_series_text
                                                                                        where device_id = @DeviceId
                                                                                        order by _ts desc
                                                                                    ) tempGrp
                                                                                ) tempSeries where row_number = 1",
                                                                            new
                                                                            {
                                                                                DeviceId = ingestionInfo.DeviceIdFromFile,
                                                                            });
            WriteLogWithStopwatch(LogLevel.Trace, $"[GetLatestMetricForDeviceAsync][{ActivityId}] Query textMetrics...");

            return (numerics, texts);
        }

        private MetricSeriesDto ProcessNumericMetricValue(MetricDataTypeDto metric, IList<string> values, int rowIndex, DeviceIngestionMetadata ingestionInfo, ProcessStatus processStatus)
        {
            bool valueValid = false;
            double metricValue = metric.DataType == DataTypeConstants.TYPE_BOOLEAN
                                ? GetMetricBoolValue(metric, values, out valueValid, rowIndex)
                                : GetMetricValue(metric, values, out valueValid, rowIndex);
            if (!valueValid)
            {
                processStatus.UpdateStatus(false);
                return null;
            }
            return new MetricSeriesDto()
            {
                Timestamp = ToStorageTimestamp(values[ingestionInfo.TimestampIndex]),
                DeviceId = ingestionInfo.DeviceIdFromFile,
                MetricKey = metric.MetricKey,
                RetentionDays = ingestionInfo.DeviceInformation.RetentionDays,
                Value = metricValue
            };
        }

        private MetricSeriesTextDto ProcessTextMetricValue(MetricDataTypeDto metric, IList<string> values, int rowIndex, DeviceIngestionMetadata ingestionInfo, ProcessStatus processStatus)
        {
            string metricValue = GetMetricValueText(metric, values);
            return new MetricSeriesTextDto()
            {
                Timestamp = ToStorageTimestamp(values[ingestionInfo.TimestampIndex]),
                DeviceId = ingestionInfo.DeviceIdFromFile,
                MetricKey = metric.MetricKey,
                RetentionDays = ingestionInfo.DeviceInformation.RetentionDays,
                Value = metricValue
            };
        }

        private async Task BulkInsertNumericSeriesAsync(ICollection<MetricSeriesDto> metricSeriesBatch, NpgsqlConnection dbConnection)
        {
            try
            {
                var chunks = DbConnectionExtension.Chunk(metricSeriesBatch, _batchOptions.InsertSize);
                foreach (var chunk in chunks)
                {
                    Func<NpgsqlBinaryImporter, Task> writeToSTDIN = async (NpgsqlBinaryImporter writer) =>
                    {
                        foreach (var record in chunk)
                        {
                            await writer.StartRowAsync();
                            await writer.WriteAsync(record.DeviceId, NpgsqlTypes.NpgsqlDbType.Varchar);
                            await writer.WriteAsync(record.MetricKey, NpgsqlTypes.NpgsqlDbType.Varchar);
                            await writer.WriteAsync(record.Value, NpgsqlTypes.NpgsqlDbType.Double);
                            await writer.WriteAsync(record.Timestamp, NpgsqlTypes.NpgsqlDbType.Timestamp);
                            await writer.WriteAsync(record.RetentionDays, NpgsqlTypes.NpgsqlDbType.Integer);
                        }
                    };

                    (var affected, var err) = await dbConnection.BulkInsertWithWriterAsync(
                            "device_metric_series(device_id, metric_key, value, _ts, retention_days)",
                            writeToSTDIN,
                            _logger);

                    if (!string.IsNullOrEmpty(err))
                    {
                        throw new GenericProcessFailedException(err, ExceptionErrorCode.ERROR_GENERIC_PROCESS_FAILED);
                        //TODO: Stopped?
                    }
                    else
                    {
                        WriteLog(LogLevel.Trace, $"[BulkInsertNumericSeriesAsync][{ActivityId}] Bulk Insert metricSeriesBatch - Success: {affected}...");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(LogLevel.Error, $"[BulkInsertNumericSeriesAsync][{ActivityId}] Bulk Insert metricSeriesBatch - Failed: {ex.Message}...", ex);
                throw;
            }

        }
        private async Task BulkInsertTextSeriesAsync(ICollection<MetricSeriesTextDto> metricSeriesTextBatch, NpgsqlConnection dbConnection)
        {
            try
            {
                var chunks = DbConnectionExtension.Chunk(metricSeriesTextBatch, _batchOptions.InsertSize);
                foreach (var chunk in chunks)
                {
                    Func<NpgsqlBinaryImporter, Task> writeToSTDIN = async (NpgsqlBinaryImporter writer) =>
                    {
                        foreach (var record in chunk)
                        {
                            await writer.StartRowAsync();
                            await writer.WriteAsync(record.DeviceId, NpgsqlTypes.NpgsqlDbType.Varchar);
                            await writer.WriteAsync(record.MetricKey, NpgsqlTypes.NpgsqlDbType.Varchar);
                            await writer.WriteAsync(record.Value, NpgsqlTypes.NpgsqlDbType.Text);
                            await writer.WriteAsync(record.Timestamp, NpgsqlTypes.NpgsqlDbType.Timestamp);
                            await writer.WriteAsync(record.RetentionDays, NpgsqlTypes.NpgsqlDbType.Integer);
                        }
                    };

                    (var affected, var err) = await dbConnection.BulkInsertWithWriterAsync(
                            "device_metric_series_text(device_id, metric_key, value, _ts, retention_days)",
                            writeToSTDIN,
                            _logger);


                    if (!string.IsNullOrEmpty(err))
                    {
                        throw new GenericProcessFailedException(err, ExceptionErrorCode.ERROR_GENERIC_PROCESS_FAILED);
                        //TODO: Stopped?
                    }
                    else
                    {
                        WriteLog(LogLevel.Trace, $"[BulkInsertTextSeriesAsync][{ActivityId}] Bulk Insert metricSeriesTextBatch - Success: {affected}...");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(LogLevel.Error, $"[BulkInsertTextSeriesAsync][{ActivityId}] Bulk Insert metricSeriesTextBatch - Failed: {ex.Message}...", ex);
                throw;
            }

        }

        private async Task InsertSnapshotAsync(IEnumerable<MetricSeriesDto> metricNumerics, IEnumerable<MetricSeriesTextDto> metricTexts, NpgsqlConnection dbConnection)
        {
            var lstSnapshot = new List<MetricSnapshotDto>();
            if (metricNumerics.Any())
            {
                lstSnapshot.AddRange(metricNumerics.Select(x => new MetricSnapshotDto()
                {
                    Timestamp = x.Timestamp,
                    DeviceId = x.DeviceId,
                    MetricKey = x.MetricKey,
                    Value = int.TryParse(x.Value.ToString(), out var outPut) ? outPut.ToString() : x.Value.ToString().TrimEnd('0')
                }));
            }

            if (metricTexts.Any())
            {
                lstSnapshot.AddRange(metricTexts.Select(x => new MetricSnapshotDto()
                {
                    Timestamp = x.Timestamp,
                    DeviceId = x.DeviceId,
                    MetricKey = x.MetricKey,
                    Value = x.Value
                }));
            }

            if (lstSnapshot.Any())
            {
                await ExecuteInsertSnapshotAsync(dbConnection, lstSnapshot);
            }
        }

        private async Task ExecuteInsertSnapshotAsync(NpgsqlConnection dbConnection, IEnumerable<MetricSnapshotDto> metricSnapshot)
        {
            try
            {
                Func<NpgsqlBinaryImporter, Task> writeToSTDIN = async (NpgsqlBinaryImporter writer) =>
                {
                    foreach (var record in metricSnapshot)
                    {
                        await writer.StartRowAsync();
                        await writer.WriteAsync(record.DeviceId, NpgsqlTypes.NpgsqlDbType.Varchar);
                        await writer.WriteAsync(record.MetricKey, NpgsqlTypes.NpgsqlDbType.Varchar);
                        await writer.WriteAsync(record.Value, NpgsqlTypes.NpgsqlDbType.Text);
                        await writer.WriteAsync(record.Timestamp, NpgsqlTypes.NpgsqlDbType.Timestamp);
                    }
                };

                (var affected, var err) = await dbConnection.BulkUpsertAsync(
                    "device_metric_snapshots",
                    "(device_id, metric_key, value, _ts)",
                    "(device_id, metric_key)",
                    "update set _ts = EXCLUDED._ts, value = EXCLUDED.value WHERE device_metric_snapshots._ts < EXCLUDED._ts",
                    writeToSTDIN,
                    _logger
                );
            }
            catch (Exception ex)
            {
                WriteLog(LogLevel.Error, $"[{nameof(ExecuteInsertSnapshotAsync)}][{ActivityId}] Failed to upsert the snapshot: {ex.Message}", ex);
                WriteLog(LogLevel.Debug, $"[{nameof(ExecuteInsertSnapshotAsync)}][{ActivityId}] Failed with Request Detail: {metricSnapshot.ToJson()}");
                throw;
            }
        }
        private async Task DownloadIngestFileAsync(string fileName, Stream outputStream)
        {
            try
            {
                await _storageService.DownloadFileToStreamAsync(fileName, outputStream);
            }
            catch
            {
                outputStream.Dispose();
            }
        }
        private void CalculateMetricValue(IEnumerable<DeviceMetricDataType> deviceMetrics, IDictionary<string, object> values)
        {
            foreach (var deviceMetric in deviceMetrics.Where(x => x.MetricType == TemplateKeyTypes.AGGREGATION))
            {
                try
                {
                    var value = _dynamicResolver.ResolveInstance("return true;", deviceMetric.ExpressionCompile).OnApply(values);
                    values[deviceMetric.MetricKey] = ParseValue(deviceMetric.DataType, value?.ToString());
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, exc.Message);
                }
            }
        }
        public static object ParseValue(string dataType, object val)
        {
            object value = val;
            if (dataType == DataTypeConstants.TYPE_DOUBLE)
            {
                if (double.TryParse(val?.ToString(), out var output))
                {
                    value = output;
                }
                else
                {
                    value = (double)0.0;
                }
            }
            else if (dataType == DataTypeConstants.TYPE_INTEGER)
            {
                try
                {
                    value = Convert.ToInt32(val);
                }
                catch
                {
                    value = 0;
                }
            }
            else if (dataType == DataTypeConstants.TYPE_BOOLEAN)
            {
                if (bool.TryParse(val?.ToString(), out var output))
                {
                    value = output;
                }
                else
                {
                    value = false;
                }
            }
            return value;
        }

        public async Task LogActivityAsync(string filePath, ActionStatus logEventStatus)
        {
            var logPayload = new ImportExportLogPayload<FileParser.ErrorTracking.Model.TrackError>(ActivityId, ENTITY_NAME, DateTime.UtcNow, ActivitiesLogEventAction.Ingest, logEventStatus)
            {
                Description = logEventStatus == ActionStatus.Success ? Constant.DescriptionMessage.INGEST_SUCCESS : Constant.DescriptionMessage.INGEST_FAIL,
                Detail = new List<ImportExportDetailPayload<FileParser.ErrorTracking.Model.TrackError>> { new FileIngestionLogDetail(filePath, _errorService.GetErrors) }
            };

            var activityMessage = logPayload.CreateLog(INITIATED, _tenantContext, _auditLogService.AppLevel);
            await _auditLogService.SendLogAsync(activityMessage);
        }

        public async Task SendFileIngestionStatusNotifyAsync(ActionStatus status, string description)
        {
            var message = new ImportNotifyPayload(ActivityId, ENTITY_NAME, DateTime.UtcNow, ActivitiesLogEventAction.Ingest, status)
            {
                Description = description
            };

            await _notificationService.SendNotifyAsync(NOTIFY_ENDPOINT, new UpnNotificationMessage(ENTITY_NAME, string.Empty, message));
        }

        public async Task<BaseResponse> ValidateIngestionDataAsync(string filePath)
        {
            WriteLog(LogLevel.Information, $"[ValidateIngestionDataAsync][{ActivityId}] Start processing for {filePath}");
            // Validate file format
            var processStatus = new ProcessStatus();
            using (var stream = new MemoryStream())
            using (var dbConnection = _readOnlyDbConnectionFactory.CreateConnection())
            {
                await DownloadIngestFileAsync(filePath, stream);
                WriteLogWithStopwatch(LogLevel.Trace, $"[ValidateIngestionDataAsync][{ActivityId}] Downloaded Ingest data from File...");
                if (!stream.CanRead)
                {
                    processStatus.UpdateStatus(false);
                }
                else
                {
                    stream.Position = 0;
                    using (var reader = new StreamReader(stream))
                    {
                        using (var csvReader = CsvHelperExtension.CreateCsvHelper(reader))
                        {
                            int rowIndex;
                            DeviceIngestionMetadata ingestionMetadata;
                            try
                            {
                                (ingestionMetadata, rowIndex) = await ProcessMetadataAsync(csvReader, processStatus);
                                WriteLog(LogLevel.Trace, $"[ValidateIngestionDataAsync][{ActivityId}] Validated metadata...");
                            }
                            catch (Exception ex)
                            {
                                WriteLog(LogLevel.Error, $"[ValidateIngestionDataAsync][{ActivityId}] Failed!", ex);
                                await LogActivityAsync(filePath, ActionStatus.Fail);
                                return new BaseResponse(false, string.Empty);
                            }

                            ValidateData(csvReader, rowIndex, ingestionMetadata, processStatus);
                        }
                    }
                }
                WriteLog(LogLevel.Information, $"[ValidateIngestionDataAsync][{ActivityId}] Completed!");
            }

            if (!processStatus.IsSuccess)
                await LogActivityAsync(filePath, ActionStatus.Fail);

            return new BaseResponse(processStatus.IsSuccess, string.Empty);
        }

        private void ValidateData(CsvReader csvReader, int rowIndex, DeviceIngestionMetadata ingestionMetadata, ProcessStatus processStatus)
        {
            var availableMetrics = ingestionMetadata.ListActiveMetrics.Where(t => SERIES_DATA_TYPES.Contains(t.DataType)).ToList();
            while (csvReader.Read())
            {
                rowIndex++;
                if (csvReader.ColumnCount != ingestionMetadata.DeviceMetricsFromFile.Count)
                {
                    processStatus.UpdateStatus(false);
                    HandleIngestionError(Constant.DescriptionMessage.DEVICE_FILE_INGESTION_INVALID_CSV_FORMAT, rowIndex, throwError: false);
                }
                IList<string> values = csvReader.Parser.Record;

                ValidateMetricValues(availableMetrics, values, rowIndex, ingestionMetadata, processStatus);
            }
        }

        private void ValidateMetricValues(
            IEnumerable<MetricDataTypeDto> availableMetrics,
            IList<string> values,
            int rowIndex,
            DeviceIngestionMetadata ingestionMetadata,
            ProcessStatus processStatus)
        {
            foreach (var metric in availableMetrics.Where(metric => metric.MetricKey != ingestionMetadata.TimestampKey))
            {
                var valueInput = values[metric.MetricIdx];
                if (string.IsNullOrWhiteSpace(valueInput) || string.IsNullOrEmpty(valueInput))
                    continue;

                if (metric.DataType == DataTypeConstants.TYPE_INTEGER || metric.DataType == DataTypeConstants.TYPE_DOUBLE)
                {
                    _ = GetMetricValue(metric, values, out bool valueValid, rowIndex);
                    if (!valueValid)
                    {
                        processStatus.UpdateStatus(false);
                        continue;
                    }
                }
                else if (metric.DataType == DataTypeConstants.TYPE_BOOLEAN)
                {
                    _ = GetMetricBoolValue(metric, values, out bool valueValid, rowIndex);
                    if (!valueValid)
                    {
                        processStatus.UpdateStatus(false);
                        continue;
                    }
                }
            }
        }

        private bool ValidateFirstLineCsvFile(CsvReader csvReader)
        {
            return csvReader.ColumnCount == 2
                && csvReader.TryGetField<string>(CSV_FORMAT__KEY_HEADER_INDEX, out var key)
                && string.Equals(key, CSV_FORMAT__KEY_HEADER, StringComparison.InvariantCultureIgnoreCase)
                && csvReader.TryGetField<string>(CSV_FORMAT__KEY_VALUE_INDEX, out var deviceId);
        }

        private double GetMetricValue(MetricDataTypeDto metric, IEnumerable<string> metricValues, out bool isSuccess, int rowIndex = 0)
        {
            isSuccess = true;
            double metricValue;
            var isValidDataType = true;

            if (metric.DataType == DataTypeConstants.TYPE_INTEGER)
            {
                isValidDataType = int.TryParse(metricValues.ElementAt(metric.MetricIdx), out int intValue);
                metricValue = intValue;
            }
            else
            {
                isValidDataType = double.TryParse(metricValues.ElementAt(metric.MetricIdx), out metricValue);
            }

            if (!isValidDataType)
            {
                _errorService.RegisterError(Constant.DescriptionMessage.DEVICE_FILE_INGESTION_INVALID_DATA_TYPE, rowIndex, metric.MetricKey);
                isSuccess = false;
            }
            return metricValue;
        }
        private int GetMetricBoolValue(MetricDataTypeDto metric, IEnumerable<string> metricValues, out bool isSuccess, int rowIndex = 0)
        {
            isSuccess = true;
            var isValidDataType = bool.TryParse(metricValues.ElementAt(metric.MetricIdx), out bool metricValueBoolean);
            if (!isValidDataType)
            {
                _errorService.RegisterError(Constant.DescriptionMessage.DEVICE_FILE_INGESTION_INVALID_DATA_TYPE, rowIndex, metric.MetricKey);
                isSuccess = false;
            }
            return metricValueBoolean ? 1 : 0;
        }
        private string GetMetricValueText(MetricDataTypeDto metric, IEnumerable<string> metricValues)
        {
            return metricValues.ElementAt(metric.MetricIdx).ToString();
        }

        private void HandleIngestionError(string message, int rowIndex, string metricName = "", bool throwError = true)
        {
            WriteLog(LogLevel.Error, $"[HandleIngestionError][{ActivityId}] Failed with message: {message} | Will throw error: {throwError}");
            _errorService.RegisterError(message, rowIndex, metricName);
            if (throwError)
                throw new InvalidFileFormatException(message);
        }

        private DateTime ToStorageTimestamp(string timestampString)
        {
            return timestampString.CutOffFloatingPointPlace().UnixTimeStampToDateTime().CutOffNanoseconds();
        }

        private void WriteLogWithStopwatch(LogLevel logLevel, string message, Exception ex = null)
        {
            _stopwatch.Stop();
            message = $"{message} - Elapsed time: {_stopwatch.ElapsedMilliseconds} ms";
            WriteLog(logLevel, message, ex);
            _stopwatch = Stopwatch.StartNew();
        }
        private void WriteLog(LogLevel logLevel, string message, Exception ex = null)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                case LogLevel.Error:
                    if (ex != null)
                        _logger.LogError(ex, message);
                    else
                        _logger.LogError(message);
                    break;
                case LogLevel.Warning:
                case LogLevel.Information:
                    _logger.LogInformation(message);
                    break;
                case LogLevel.Debug:
                    _logger.LogDebug(message);
                    break;
                case LogLevel.Trace:
                    _logger.LogTrace(message);
                    break;
                //case LogLevel.None:
                default:
                    break;
            }
        }
    }

    internal class ProcessStatus
    {
        public bool IsSuccess { get; private set; } = true;

        public void UpdateStatus(bool currentProcessStatus)
        {
            if (IsSuccess && !currentProcessStatus) // only allow update from true => false
                IsSuccess = false;
        }
    }

    internal class DeviceIngestionMetadata
    {
        public string DeviceIdFromFile { get; }
        public int TimestampIndex { get; }
        public string TimestampKey { get; }
        public DeviceInformation DeviceInformation { get; }
        public IList<string> DeviceMetricsFromFile { get; }
        public IEnumerable<MetricDataTypeDto> ListActiveMetrics { get; }

        public DeviceIngestionMetadata(
            string deviceIdFromFile,
            int timestampIndex,
            string timestampKey,
            DeviceInformation deviceInformation,
            IList<string> deviceMetricsFromFile,
            IEnumerable<MetricDataTypeDto> listActiveMetrics)
        {
            DeviceIdFromFile = deviceIdFromFile;
            TimestampIndex = timestampIndex;
            TimestampKey = timestampKey;
            DeviceInformation = deviceInformation;
            DeviceMetricsFromFile = deviceMetricsFromFile;
            ListActiveMetrics = listActiveMetrics;
        }
    }
    internal class AssetAttributeDynamicDto
    {
        public Guid AssetId { get; set; }
        public Guid AttributeId { get; set; }
    }
    internal class MetricSeriesDto
    {
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public double Value { get; set; }
        public int RetentionDays { get; set; }
    }
    internal class MetricSeriesTextDto
    {
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public string Value { get; set; }
        public int RetentionDays { get; set; }
    }

    internal class MetricDataTypeDto
    {
        public string MetricKey { get; set; }
        public string DataType { get; set; }
        public DateTime? Timestamp { get; set; }
        public int MetricIdx { get; set; }
    }
    internal class MetricSnapshotDto
    {
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; set; }
        public string MetricKey { get; set; }
        public object Value { get; set; }
    }
}