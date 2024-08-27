using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Model;
using AHI.Infrastructure.Repository.Abstraction.ReadOnly;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Interceptor.Abstraction;
using AHI.Infrastructure.Repository.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Function.Extension;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AHI.Device.Function.Service
{
    public class IngestionProcessEventService : IIngestionProcessEventService
    {
        protected readonly IConfiguration _configuration;
        protected readonly IDomainEventDispatcher _domainEventDispatcher;
        protected readonly IReadOnlyDeviceRepository _readOnlyDeviceRepository;
        protected readonly ILogger<IngestionProcessEventService> _logger;
        protected readonly IDynamicResolver _dynamicResolver;
        protected string[] RESERVE_KEYS = new[] { "tenantId", "subscriptionId", "projectId", "deviceId", "integrationId" };
        protected readonly IDbConnectionResolver _dbConnectionResolver;
        private readonly IServiceProvider _serviceProvider;


        public IngestionProcessEventService(
            IDomainEventDispatcher domainEventDispatcher,
            IReadOnlyDeviceRepository readOnlyDeviceRepository,
            IConfiguration configuration,
            ILogger<IngestionProcessEventService> logger,
            IDynamicResolver dynamicResolver,
            IDbConnectionResolver dbConnectionResolver,
            IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _domainEventDispatcher = domainEventDispatcher;
            _readOnlyDeviceRepository = readOnlyDeviceRepository;
            _logger = logger;
            _dynamicResolver = dynamicResolver;
            _dbConnectionResolver = dbConnectionResolver;
            _serviceProvider = serviceProvider;
        }

        public async Task ProcessEventAsync(IDictionary<string, object> metricDict)
        {
            var projectId = metricDict[MetricPayload.PROJECT_ID] as string;

            //save list device to cache
            _ = await GetListDeviceInformationAsync(metricDict, projectId);

            if (metricDict.ContainsKey(MetricPayload.INTEGRATION_ID))
            {
                var integrationDeviceService = _serviceProvider.GetRequiredService<IIntegrationDeviceCalculateRuntimeMetricService>();
                await integrationDeviceService.CalculateRuntimeMetricAsync(metricDict);
            }
            else
            {
                var calculateRuntimeMetricService = _serviceProvider.GetRequiredService<ICalculateRuntimeMetricService>();
                await calculateRuntimeMetricService.CalculateRuntimeMetricAsync(metricDict);
            }

            var calculateRuntimeAttributeService = _serviceProvider.GetRequiredService<ICalculateRuntimeAttributeService>();
            await calculateRuntimeAttributeService.CalculateRuntimeAttributeAsync(metricDict);
        }

        public async Task<IEnumerable<DeviceInformation>> GetListDeviceInformationAsync(IDictionary<string, object> metricDict, string projectId)
        {
            var listDeviceInformation = new List<DeviceInformation>();

            //Check deviceId exist in payload before check other payload map with templateid
            if (metricDict.ContainsKey(MetricPayload.DEVICE_ID) && !string.IsNullOrEmpty(metricDict[MetricPayload.DEVICE_ID]?.ToString()))
            {
                var deviceInformation = await _readOnlyDeviceRepository.GetDeviceInformationAsync(projectId, new string[] { metricDict[MetricPayload.DEVICE_ID]?.ToString() });

                if (deviceInformation is not null)
                {
                    listDeviceInformation.Add(deviceInformation);
                }
            }
            else if (metricDict.ContainsKey(MetricPayload.TOPIC_NAME) && !string.IsNullOrEmpty(metricDict[MetricPayload.TOPIC_NAME]?.ToString()))
            {
                var deviceInformations = await GetDeviceInformationsWithTopicNameAsync(metricDict);
                if (deviceInformations is not null)
                {
                    listDeviceInformation.AddRange(deviceInformations);
                }
            }
            else
            {
                var deviceInformation = await GetDeviceInformationFromMetricKeyAsync(projectId, metricDict);

                if (deviceInformation is not null)
                {
                    listDeviceInformation.Add(deviceInformation);
                }
            }

            return listDeviceInformation;
        }

        private async Task<DeviceInformation> GetDeviceInformationFromMetricKeyAsync(string projectId, IDictionary<string, object> metricDict)
        {
            DeviceInformation deviceInformation = null;

            if (string.IsNullOrWhiteSpace(projectId) || !Guid.TryParse(projectId, out Guid projectIdGuid))
            {
                _logger.LogInformation("ProjectId is invalid.");
                return null;
            }
            // the device metric key hardly change, implement memory cache should be sufficient
            var deviceIdKeys = await _readOnlyDeviceRepository.GetDeviceMetricKeyAsync(projectId);
            if (!deviceIdKeys.Any())
            {
                _logger.LogInformation("No key define");
                return null;
            }

            //// the case:
            //// multiple deviceId match in database
            //// need to loop all the possible deviceId and find the right one
            var deviceIds = (from d in deviceIdKeys
                             join metric in metricDict on d.ToLowerInvariant() equals metric.Key.ToLowerInvariant()
                             select metric.Value.ToString()
                            ).ToArray();
            if (deviceIds.Length == 0)
            {
                _logger.LogInformation($"Can not process message because no key is matched, key list [{string.Join(",", deviceIdKeys)}]");
                return null;
            }
            _logger.LogTrace($"Possible DeviceIds {string.Join(",", deviceIds)}");

            deviceInformation = await _readOnlyDeviceRepository.GetDeviceInformationAsync(projectId, deviceIds);

            return deviceInformation;
        }

        protected async Task<IEnumerable<string>> GetListDeviceId(IDictionary<string, object> metricDict)
        {
            var listDeviceId = new List<string>();
            if (metricDict.ContainsKey(MetricPayload.DEVICE_ID) && !string.IsNullOrEmpty(metricDict[MetricPayload.DEVICE_ID]?.ToString()))
            {
                listDeviceId.Add(metricDict[MetricPayload.DEVICE_ID].ToString());
            }
            else if (metricDict.ContainsKey(MetricPayload.TOPIC_NAME) && !string.IsNullOrEmpty(metricDict[MetricPayload.TOPIC_NAME]?.ToString()))
            {
                var deviceInformations = await GetDeviceInformationsWithTopicNameAsync(metricDict);
                if (deviceInformations is not null)
                {
                    listDeviceId.AddRange(deviceInformations.Select(c => c.DeviceId));
                }
            }

            return listDeviceId;
        }

        protected async Task<IEnumerable<DeviceInformation>> GetDeviceInformationsWithTopicNameAsync(IDictionary<string, object> metricDict)
        {
            var projectId = metricDict[Constant.MetricPayload.PROJECT_ID]?.ToString();
            var topicName = metricDict[MetricPayload.TOPIC_NAME]?.ToString();
            var brokerType = metricDict[MetricPayload.BROKER_TYPE]?.ToString();

            IEnumerable<DeviceInformation> deviceInformation = null;

            if (!string.IsNullOrEmpty(topicName))
                deviceInformation = await _readOnlyDeviceRepository.GetDeviceInformationsWithTopicNameAsync(projectId, topicName, brokerType);

            return deviceInformation;
        }

        protected IEnumerable<MetricValue> GetTotalSnapshotMetrics(IDictionary<string, object> metricDict, DeviceInformation deviceInformation)
        {
            var snapshotMetrics = GetSnapshotMetrics(metricDict, deviceInformation);

            if (snapshotMetrics != null)
            {
                var unixTimestamp = GetUnixTimestamp(deviceInformation, metricDict);

                var projectId = metricDict[Constant.MetricPayload.PROJECT_ID] as string;
                var calculatedMetrics = GetCalculateMetrics(metricDict, deviceInformation, unixTimestamp, projectId);

                snapshotMetrics = snapshotMetrics.Union(calculatedMetrics);
            }

            return snapshotMetrics;
        }

        protected IEnumerable<MetricValue> GetSnapshotMetrics(IDictionary<string, object> metricDict, DeviceInformation deviceInformation)
        {
            //var (deviceId, deviceMetrics) = await _deviceRepository.GetDeviceMetricDetailAsync(projectId, deviceIds);
            if (deviceInformation == null || !deviceInformation.Metrics.Any())
            {
                return null;
            }

            var unixTimestamp = GetUnixTimestamp(deviceInformation, metricDict);

            var metrics = FlattenMetrics(metricDict, deviceInformation, unixTimestamp);
            // var metricValues = metrics.Select(x => new
            // {
            //     MetricKey = x.MetricKey,
            //     Value = x.Value,
            //     Timestamp = x.Timestamp,
            //     UnixTimestamp = x.UnixTimestamp,
            //     DeviceId = deviceInformation.DeviceId
            // });

            var snapshotMetrics = metrics.GroupBy(x => x.MetricKey).Select(x =>
            {
                return x.OrderByDescending(m => m.UnixTimestamp).First();
                //var lastestSnapshot = x.OrderByDescending(m => m.UnixTimestamp).First();
                //return new MetricValue(deviceInformation.DeviceId, x.Key, lastestSnapshot.UnixTimestamp, lastestSnapshot.Timestamp, lastestSnapshot.Value, lastestSnapshot.DataType);
            });

            return snapshotMetrics;
        }

        protected long GetUnixTimestamp(DeviceInformation deviceInformation, IDictionary<string, object> metricDict)
        {
            var timestampKeys = deviceInformation.Metrics.Where(x => x.MetricType == TemplateKeyTypes.TIMESTAMP).Select(x => x.MetricKey);
            string timestamp = (from ts in timestampKeys
                                join metric in metricDict on ts.ToLowerInvariant() equals metric.Key.ToLowerInvariant()
                                select metric.Value.ToString()
                                ).FirstOrDefault();

            //var timestampKeys = await _deviceRepository.GetTimestampKeyAsync(projectId, deviceId);

            if (!long.TryParse(timestamp, out var unixTimestamp) && !string.IsNullOrEmpty(timestamp))
            {
                var deviceTimestamp = timestamp.CutOffFloatingPointPlace().UnixTimeStampToDateTime().CutOffNanoseconds();
                unixTimestamp = (new DateTimeOffset(deviceTimestamp)).ToUnixTimeMilliseconds();
            }

            return unixTimestamp;
        }

        protected IDbConnection GetReadOnlyDbConnection(string projectId) => _dbConnectionResolver.CreateConnection(projectId, true);
        protected IDbConnection GetWriteDbConnection(string projectId) => _dbConnectionResolver.CreateConnection(projectId);

        protected IEnumerable<MetricValue> FlattenMetrics(IDictionary<string, object> metricDict, DeviceInformation deviceInformation, long? timestamp)
        {
            var metricValues = new List<MetricValue>();
            var deviceId = deviceInformation.DeviceId;
            var metrics = (from metric in metricDict
                            join m in deviceInformation.Metrics on metric.Key.ToLowerInvariant() equals m.MetricKey.ToLowerInvariant()
                            where !RESERVE_KEYS.Contains(metric.Key) && m.MetricType != TemplateKeyTypes.TIMESTAMP
                            select new { Key = m.MetricKey, metric.Value, DataType = m.DataType }
                        );
            foreach (var m in metrics)
            {
                if (m.Value.TryParseJArray<IEnumerable<string[]>>(out var result))
                {
                    foreach (var data in result)
                    {
                        var ts = data[0];
                        var value = data[1];
                        if (long.TryParse(ts, out var unixTimestamp))
                        {
                            var metricValue = new MetricValue(deviceId, m.Key, unixTimestamp, value, m.DataType);
                            metricValues.Add(metricValue);
                        }
                    }
                }
                // signal value must have timestamp
                else if (timestamp > 0)
                {
                    var metricValue = new MetricValue(deviceId, m.Key, timestamp.Value, m.Value.ToString(), m.DataType);
                    metricValues.Add(metricValue);
                }
            }
            return metricValues;
        }

        protected object ParseValue(string dataType, string val)
        {
            object value = val;
            if (dataType == DataTypeConstants.TYPE_DOUBLE)
            {
                if (double.TryParse(val, out var output))
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
                // in this case: 29.00121 -> can be failed
                if (Int32.TryParse(val, out var output))
                {
                    value = output;
                }
                // fallback to double and then cast to integer
                else if (double.TryParse(val, out var doubleValue))
                {
                    value = (int)Math.Round(doubleValue);
                }
                else
                {
                    value = 0;
                }
            }
            else if (dataType == DataTypeConstants.TYPE_BOOLEAN)
            {
                if (bool.TryParse(val, out var output))
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

        protected object ParseValueToStore(string dataType, object value)
        {
            if (value != null)
            {
                if (dataType == DataTypeConstants.TYPE_DOUBLE)
                {
                    if (double.TryParse(value.ToString(), out var doubleValue))
                    {
                        return doubleValue;
                    }
                }
                else if (dataType == DataTypeConstants.TYPE_INTEGER)
                {
                    //need parse to double first cause string like 1.000000000000 cant parse to int
                    if (double.TryParse(value.ToString(), out var intValue))
                    {
                        return (int)Math.Round(intValue);
                    }
                }
                else if (dataType == DataTypeConstants.TYPE_BOOLEAN)
                {
                    if (bool.TryParse(value.ToString(), out var boolValue))
                    {
                        return boolValue ? 1 : 0;
                    }
                }
                else
                {
                    return value.ToString();
                }
            }
            return 0.0;
        }

        protected IEnumerable<MetricValue> GetCalculateMetrics(IDictionary<string, object> metricDict, DeviceInformation deviceInformation, long unixTimestamp, string projectId)
        {
            var snapshotMetrics = GetSnapshotMetrics(metricDict, deviceInformation);

            if (snapshotMetrics == null || !snapshotMetrics.Any())
            {
                _logger.LogError($"Possible SnapshotMetrics is empty. Project: {projectId}");
                return Enumerable.Empty<MetricValue>();
            }

            var fromDeviceMetrics = snapshotMetrics.Select(x => x.MetricKey);
            var values = deviceInformation.Metrics.Where(x => !fromDeviceMetrics.Contains(x.MetricKey)).ToDictionary(x => x.MetricKey, y => ParseValue(y.DataType, y.Value));

            // override from device data
            foreach (var metric in snapshotMetrics)
            {
                values[metric.MetricKey] = ParseValue(metric.DataType, metric.Value);
            }

            var calculatedMetrics = CalculateRuntimeValue(deviceInformation, unixTimestamp, values);

            return calculatedMetrics;
        }

        protected IEnumerable<MetricValue> CalculateRuntimeValue(DeviceInformation deviceInformation, long unixTimestamp, IDictionary<string, object> values)
        {
            var calculatedMetrics = new List<MetricValue>();
            foreach (var deviceMetric in deviceInformation.Metrics.Where(x => x.MetricType == TemplateKeyTypes.AGGREGATION))
            {
                try
                {
                    var value = _dynamicResolver.ResolveInstance("return true;", deviceMetric.ExpressionCompile).OnApply(values);
                    var valueAsString = value?.ToString();
                    var val = ParseValue(deviceMetric.DataType, valueAsString);
                    values[deviceMetric.MetricKey] = val;
                    calculatedMetrics.Add(new MetricValue(deviceInformation.DeviceId, deviceMetric.MetricKey, unixTimestamp, valueAsString, deviceMetric.DataType));
                }
                catch (System.Exception exc)
                {
                    _logger.LogError(exc, exc.Message);
                }
            }
            return calculatedMetrics;
        }
    }

    public class AttributeSnapshot
    {
        public Guid AttributeId { get; set; }
        public string Value { get; set; }
        public string DataType { get; set; }
        public DateTime? Timestamp { get; set; }
    }
}