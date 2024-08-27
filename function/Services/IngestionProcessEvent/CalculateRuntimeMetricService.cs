using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Events;
using AHI.Device.Function.Model;
using AHI.Infrastructure.Repository.Abstraction.ReadOnly;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Interceptor.Abstraction;
using AHI.Infrastructure.Repository.Abstraction;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data.Common;
using Function.Helper;
using AHI.Infrastructure.SharedKernel.Extension;
using Microsoft.Extensions.Logging;
using Function.Extension;

namespace AHI.Device.Function.Service
{
    public class CalculateRuntimeMetricService : IngestionProcessEventService, ICalculateRuntimeMetricService
    {
        private readonly ILogger<CalculateRuntimeMetricService> _loggerChildren;
        private const int SIGNAL_QUALITY_CODE_GOOD = 192;

        public CalculateRuntimeMetricService(
            IDomainEventDispatcher domainEventDispatcher,
            IReadOnlyDeviceRepository readOnlyDeviceRepository,
            IConfiguration configuration,
            ILogger<IngestionProcessEventService> logger,
            ILogger<CalculateRuntimeMetricService> loggerChildren,
            IDynamicResolver dynamicResolver,
            IDbConnectionResolver dbConnectionResolver,
            IServiceProvider serviceProvider) : base(domainEventDispatcher, readOnlyDeviceRepository, configuration, logger, dynamicResolver, dbConnectionResolver, serviceProvider)
        {
            _loggerChildren = loggerChildren;
        }


        public async Task CalculateRuntimeMetricAsync(IDictionary<string, object> metricDict)
        {
            var projectId = metricDict[MetricPayload.PROJECT_ID] as string;
            var listDeviceInformation = await GetListDeviceInformationAsync(metricDict, projectId);
            var connection = GetWriteDbConnection(projectId);
            connection.Open();
            try
            {
                await StoreSnapshotMetricsAsync(connection, metricDict, projectId, listDeviceInformation);
                await StoreNumericValueAsync(connection, metricDict, projectId, listDeviceInformation);
                await StoreTextValuesAsync(connection, metricDict, projectId, listDeviceInformation);
                connection.Close();
                _logger.LogInformation("CalculateRuntimeMetricAsync: finish");
            }
            catch (Exception e)
            {
                connection.Close();
                _logger.LogError(e, "CalculateRuntimeMetricAsync: failure: metric={metric}", metricDict.ToJson());
            }
        }

        private async Task StoreSnapshotMetricsAsync(IDbConnection dbConnection, IDictionary<string, object> metricDict, string projectId, IEnumerable<DeviceInformation> listDeviceInformation)
        {
            try
            {
                foreach (var deviceInformation in listDeviceInformation)
                {
                    var snapshotMetrics = GetTotalSnapshotMetrics(metricDict, deviceInformation);

                    if (snapshotMetrics == null || !snapshotMetrics.Any())
                    {
                        _logger.LogError($"Possible SnapshotMetrics is empty. Project: {projectId} / deviceID: {deviceInformation.DeviceId}");
                        continue;
                    }

                    _logger.LogDebug($"StoreSnapshotMetricsAsync - snapshotMetrics = {snapshotMetrics.ToJson()}");
                    //TODO: To check again if we should apply the transaction scope for all devices instead of each device like current.
                    using (var transaction = dbConnection.BeginTransaction())
                    {
                        try
                        {
                            await dbConnection.ExecuteAsync($@"INSERT INTO device_metric_snapshots(_ts, device_id, metric_key, value)
                                                VALUES(@Timestamp, @DeviceId, @MetricKey, @Value)
                                                ON CONFLICT (device_id, metric_key)
                                                DO UPDATE SET _ts = EXCLUDED._ts, value = EXCLUDED.value WHERE device_metric_snapshots._ts < EXCLUDED._ts;
                                                ", snapshotMetrics);

                            var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategySync(_logger);
                            retryStrategy.Execute(() => transaction.Commit());
                        }
                        catch (DbException ex)
                        {
                            _logger.LogError(ex, $"StoreSnapshotMetricsAsync DbException - snapshotMetrics = {snapshotMetrics.ToJson()}");
                            transaction.Rollback();
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"StoreSnapshotMetricsAsync Exception - snapshotMetrics = {snapshotMetrics.ToJson()}");
                            transaction.Rollback();
                            throw;
                        }
                    }

                    if (deviceInformation.EnableHealthCheck)
                    {
                        // tracking the device heart beat
                        await _domainEventDispatcher.SendAsync(new TrackingHeartBeatEvent(metricDict));
                    }
                }

            }
            catch (Exception ex)
            {
                _loggerChildren.LogError(ex, ex.Message);
            }

        }

        private async Task StoreNumericValueAsync(
            IDbConnection dbConnection,
            IDictionary<string, object> metricDict,
            string projectId,
            IEnumerable<DeviceInformation> listDeviceInformation)
        {
            try
            {
                foreach (var deviceInformation in listDeviceInformation)
                {
                    var snapshotValues = GetSnapshotValues(metricDict, deviceInformation, projectId);
                    var numericValues = snapshotValues.Where(dm => DataTypeExtensions.IsNumericTypeSeries(dm.DataType))
                                                    .Select(att => new
                                                    {
                                                        Timestamp = att.Timestamp,
                                                        DeviceId = att.DeviceId,
                                                        MetricKey = att.MetricKey,
                                                        Value = ParseValueToStore(att.DataType, att.Value),
                                                        SignalQualityCode = SIGNAL_QUALITY_CODE_GOOD,
                                                        RetentionDays = deviceInformation.RetentionDays
                                                    })
                                                    .OrderBy(x => x.Timestamp);

                    if (!numericValues.Any())
                    {
                        _logger.LogTrace($"StoreNumericValueAsync - No numericValues found. Project: {projectId} / deviceID: {deviceInformation.DeviceId}!");
                        continue;
                    }

                    _logger.LogDebug($"StoreNumericValueAsync - numericValues = {numericValues.ToJson()}");
                    using (var transaction = dbConnection.BeginTransaction())
                    {
                        try
                        {
                            await dbConnection.ExecuteAsync($@"
                                                INSERT INTO device_metric_series(_ts, device_id, metric_key, value, signal_quality_code, retention_days)
                                                VALUES (@Timestamp, @DeviceId, @MetricKey, @Value,@SignalQualityCode, @RetentionDays );
                                                ", numericValues);

                            var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategySync(_logger);
                            retryStrategy.Execute(() => transaction.Commit());
                        }
                        catch (DbException ex)
                        {
                            _logger.LogError(ex, $"StoreNumericValueAsync DbException - numericValues = {numericValues.ToJson()}");
                            transaction.Rollback();
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"StoreNumericValueAsync Exception - numericValues = {numericValues.ToJson()}");
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggerChildren.LogError(ex, ex.Message);
            }
        }

        private async Task StoreTextValuesAsync(
            IDbConnection dbConnection,
            IDictionary<string, object> metricDict,
            string projectId,
            IEnumerable<DeviceInformation> listDeviceInformation)
        {
            try
            {
                foreach (var deviceInformation in listDeviceInformation)
                {
                    var snapshotValues = GetSnapshotValues(metricDict, deviceInformation, projectId);
                    var textValues = snapshotValues.Where(dm => DataTypeExtensions.IsTextTypeSeries(dm.DataType))
                                                    .Select(att => new
                                                    {
                                                        Timestamp = att.Timestamp,
                                                        DeviceId = att.DeviceId,
                                                        MetricKey = att.MetricKey,
                                                        Value = ParseValueToStore(att.DataType, att.Value),
                                                        SignalQualityCode = SIGNAL_QUALITY_CODE_GOOD,
                                                        RetentionDays = deviceInformation.RetentionDays
                                                    })
                                                    .OrderBy(x => x.Timestamp);

                    if (!textValues.Any())
                    {
                        _logger.LogTrace($"StoreTextValuesAsync - No textValues found. Project: {projectId} / deviceID: {deviceInformation.DeviceId}!");
                        continue;
                    }

                    _logger.LogDebug($"StoreTextValuesAsync - textValues = {textValues.ToJson()}");
                    using (var transaction = dbConnection.BeginTransaction())
                    {
                        try
                        {
                            await dbConnection.ExecuteAsync($@"
                                                INSERT INTO device_metric_series_text(_ts, device_id, metric_key, value, signal_quality_code, retention_days)
                                                VALUES (@Timestamp, @DeviceId, @MetricKey, @Value, @SignalQualityCode, @RetentionDays);
                                                ", textValues);

                            var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategySync(_logger);
                            retryStrategy.Execute(() => transaction.Commit());
                        }
                        catch (DbException ex)
                        {
                            _logger.LogError(ex, $"StoreTextValuesAsync DbException - textValues = {textValues.ToJson()}");
                            transaction.Rollback();
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"StoreTextValuesAsync Exception - textValues = {textValues.ToJson()}");
                            transaction.Rollback();
                            throw;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                _loggerChildren.LogError(ex, ex.Message);
            }
        }
        private IEnumerable<MetricValue> GetSnapshotValues(
            IDictionary<string, object> metricDict,
            DeviceInformation deviceInformation,
            string projectId)
        {
            var unixTimestamp = GetUnixTimestamp(deviceInformation, metricDict);

            var calculatedMetrics = GetCalculateMetrics(metricDict, deviceInformation, unixTimestamp, projectId);

            var metrics = FlattenMetrics(metricDict, deviceInformation, unixTimestamp);

            var snapshotValues = metrics.Union(calculatedMetrics);

            return snapshotValues;
        }
    }

    public class RuntimeValueObject
    {
        public DateTime Timestamp { get; set; }
        public Guid AssetId { get; set; }
        public Guid AttributeId { get; set; }
        public object Value { get; set; }
        public string DataType { get; set; }
        public int RetentionDays { get; set; }
    }
}