using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Model;
using AHI.Infrastructure.Repository.Abstraction.ReadOnly;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Function.Service.Model;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.Interceptor.Abstraction;
using AHI.Infrastructure.Repository.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Dapper;
using Function.Extension;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using DateTimeExtensions = Function.Extension.DateTimeExtensions;
using Function.Helper;
using Microsoft.Extensions.Logging;

namespace AHI.Device.Function.Service
{
    public class CalculateRuntimeAttributeService : IngestionProcessEventService, ICalculateRuntimeAttributeService
    {
        private readonly IReadOnlyAssetRepository _readOnlyAssetRepository;
        private readonly IDeviceRepository _deviceRepository;
        private readonly ICache _cache;
        private readonly IFowardingNotificationService _forwardingNotificationService;

        public CalculateRuntimeAttributeService(
            IDomainEventDispatcher domainEventDispatcher,
            IReadOnlyDeviceRepository readOnlyDeviceRepository,
            IConfiguration configuration,
            ILogger<IngestionProcessEventService> logger,
            IDynamicResolver dynamicResolver,
            IReadOnlyAssetRepository readOnlyAssetRepository,
            ICache cache,
            IFowardingNotificationService forwardingNotificationService,
            IDeviceRepository deviceRepository,
            IDbConnectionResolver dbConnectionResolver,
            IServiceProvider serviceProvider) : base(domainEventDispatcher, readOnlyDeviceRepository, configuration, logger, dynamicResolver, dbConnectionResolver, serviceProvider)
        {
            _readOnlyAssetRepository = readOnlyAssetRepository;
            _cache = cache;
            _forwardingNotificationService = forwardingNotificationService;
            _deviceRepository = deviceRepository;
        }

        public async Task CalculateRuntimeAttributeAsync(IDictionary<string, object> metricDict)
        {
            var projectId = metricDict[Constant.MetricPayload.PROJECT_ID] as string;
            var listDeviceInformation = await GetListDeviceInformationAsync(metricDict, projectId);

            var (dataAssetAttributes, dataAssetRuntimeTriggers, dataRuntimeValues, dataUnixTimestamps) = await GetAllDataAssetRuntime(metricDict, listDeviceInformation, projectId);

            var runtimeValues = dataRuntimeValues.SelectMany(c => c.Value);

            _logger.LogDebug($"CalculateRuntimeAttributeAsync - runtimeValues = {runtimeValues.ToJson()}");
            var dbConnection = GetWriteDbConnection(projectId);
            dbConnection.Open();
            try
            {
                await StoreAliasKeyToRedisAsync(projectId, listDeviceInformation, dataAssetAttributes, dataAssetRuntimeTriggers);
                await StoreSnapshotAttributesAsync(dbConnection, projectId, runtimeValues);
                await StoreSnapshotNumericsAsync(dbConnection, projectId, runtimeValues);
                await StoreSnapshotTextsAsync(dbConnection, projectId, runtimeValues);
                dbConnection.Close();
                await _forwardingNotificationService.ForwardingNotificationAssetMessageAsync(metricDict, listDeviceInformation, dataUnixTimestamps, dataRuntimeValues);
                _logger.LogInformation("CalculateRuntimeAttributeAsync finish");
            }
            catch (Exception e)
            {
                dbConnection.Close();
                _logger.LogError(e, "CalculateRuntimeAttributeAsync failure: metric={metric}", metricDict.ToJson());
            }

        }

        private async Task StoreAliasKeyToRedisAsync(
            string projectId,
            IEnumerable<DeviceInformation> listDeviceInformation,
            Dictionary<string, IEnumerable<AssetAttribute>> dataAssetAttributes,
            Dictionary<string, IEnumerable<AssetRuntimeTrigger>> dataAssetRuntimeTriggers)
        {
            var tasks = listDeviceInformation.Select(async deviceInformation =>
            {
                var deviceId = deviceInformation.DeviceId;

                if (!dataAssetRuntimeTriggers.ContainsKey(deviceId))
                {
                    _logger.LogError($"assetRuntimeTriggers is empty. Device: {deviceId}");
                    return;
                }

                var assetAttributes = dataAssetAttributes[deviceId];

                foreach (var alias in assetAttributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS))
                {
                    var aliasFailedHashField = CacheKey.PROCESSING_FAILED_HASH_FIELD.GetCacheKey(alias.AssetId, alias.AttributeId);
                    var aliasFailedHashKey = CacheKey.PROCESSING_FAILED_HASH_KEY.GetCacheKey(projectId);

                    var aliasHashField = CacheKey.PROCESSING_ASSET_HASH_FIELD.GetCacheKey(alias.AssetId, alias.AttributeId);
                    var aliasHashKey = CacheKey.PROCESSING_ASSET_HASH_KEY.GetCacheKey(projectId);

                    try
                    {
                        string aliasFailedHit = await _cache.GetHashByKeyInStringAsync(aliasFailedHashKey, aliasFailedHashField);
                        if (!string.IsNullOrEmpty(aliasFailedHit))
                        {
                            _logger.LogDebug("Alias failed - from cache. System will ignore until user modified the value");
                            continue;
                        }

                        string targetAliasAttributeIdString = await _cache.GetHashByKeyInStringAsync(aliasHashKey, aliasHashField);
                        if (targetAliasAttributeIdString == null)
                        {
                            using var dbConnection = GetReadOnlyDbConnection(projectId);
                            var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                            var targetAliasAttributeId = await retryStrategy.ExecuteAsync(async () =>
                                await dbConnection.QuerySingleOrDefaultAsync<Guid>(@"
                                    select attribute_id 
                                    from find_root_alias_asset_attribute(@AliasAttributeId) 
                                    order by alias_level desc limit 1",
                                    new { AliasAttributeId = alias.AttributeId },
                                    commandTimeout: 2)
                            );

                            dbConnection.Close();

                            targetAliasAttributeIdString = targetAliasAttributeId.ToString();
                            await _cache.SetHashByKeyAsync(aliasHashKey, aliasHashField, targetAliasAttributeIdString);
                        }
                        else
                        {
                            // nothing change. no need to update
                            _logger.LogDebug("StoreAliasKeyToRedisAsync - Cache hit, no change");
                        }
                    }
                    catch (Exception exc)
                    {
                        await _cache.SetHashByKeyAsync(aliasFailedHashKey, aliasFailedHashField, exc.Message, TimeSpan.FromDays(1));
                        _logger.LogError(exc, exc.Message);
                    }
                }
            });

            await Task.WhenAll(tasks);
        }

        private async Task StoreSnapshotAttributesAsync(IDbConnection dbConnection, string projectId, IEnumerable<RuntimeValueObject> runtimeValues)
        {
            if (!runtimeValues.Any())
            {
                _logger.LogTrace($"StoreSnapshotAttributesAsync - No snapshot value found!");
                return;
            }

            using (var transaction = dbConnection.BeginTransaction())
            {
                try
                {
                    _logger.LogDebug($"store asset_attribute_runtime_snapshots runtimeValues = {runtimeValues.ToJson()}");
                    await dbConnection.ExecuteAsync($@"INSERT INTO asset_attribute_runtime_snapshots(_ts, asset_id, asset_attribute_id, value)
                                            VALUES(@Timestamp, @AssetId, @AttributeId, @Value)
                                            ON CONFLICT (asset_id, asset_attribute_id)
                                            DO UPDATE SET _ts = EXCLUDED._ts, value = EXCLUDED.value WHERE asset_attribute_runtime_snapshots._ts < EXCLUDED._ts;
                                            ", runtimeValues);


                    var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategySync(_logger);
                    retryStrategy.Execute(() => transaction.Commit());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"StoreSnapshotAttributesAsync Exception - runtimeValues = {runtimeValues.ToJson()}");
                    transaction.Rollback();
                }
            }
        }

        private async Task StoreSnapshotNumericsAsync(IDbConnection dbConnection, string projectId, IEnumerable<RuntimeValueObject> runtimeValues)
        {
            // numeric value
            var numericValues = runtimeValues.Where(x => DataTypeExtensions.IsNumericTypeSeries(x.DataType));
            if (!numericValues.Any())
            {
                _logger.LogTrace($"StoreSnapshotNumericsAsync - No numeric value found!");
                return;
            }

            using (var transaction = dbConnection.BeginTransaction())
            {
                try
                {
                    _logger.LogDebug($"store asset_attribute_runtime_series runtimeValues = {numericValues.ToJson()}");
                    await dbConnection.ExecuteAsync($@" INSERT INTO asset_attribute_runtime_series(_ts, asset_id, asset_attribute_id, value, retention_days)
                                    VALUES (@Timestamp, @AssetId, @AttributeId, @Value, @RetentionDays);
                                    ", numericValues);

                    var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategySync(_logger);
                    retryStrategy.Execute(() => transaction.Commit());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"StoreSnapshotNumericsAsync Exception - numericValues = {numericValues.ToJson()}");
                    transaction.Rollback();
                }
            }
        }

        private async Task StoreSnapshotTextsAsync(IDbConnection dbConnection, string projectId, IEnumerable<RuntimeValueObject> runtimeValues)
        {
            // text value
            var textValues = runtimeValues.Where(x => DataTypeExtensions.IsTextTypeSeries(x.DataType)).Select(x => new
            {
                Timestamp = x.Timestamp,
                AssetId = x.AssetId,
                AttributeId = x.AttributeId,
                Value = x.Value.ToString(), // should be a string
                DataType = x.DataType,
                RetentionDays = x.RetentionDays,
            });

            if (!textValues.Any())
            {
                _logger.LogTrace($"StoreSnapshotTextsAsync - No text value found!");
                return;
            }

            using (var transaction = dbConnection.BeginTransaction())
            {
                try
                {
                    _logger.LogDebug($"store asset_attribute_runtime_series_text runtimeValues = {textValues.ToJson()}");
                    await dbConnection.ExecuteAsync($@" INSERT INTO asset_attribute_runtime_series_text(_ts, asset_id, asset_attribute_id, value, retention_days)
                                        VALUES (@Timestamp, @AssetId, @AttributeId, @Value, @RetentionDays);
                                        ", textValues);

                    var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategySync(_logger);
                    retryStrategy.Execute(() => transaction.Commit());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"StoreSnapshotTextsAsync Exception - textValues = {textValues.ToJson()}");
                    transaction.Rollback();
                }
            }
        }

        private async Task<IEnumerable<RuntimeValueObject>> GetRuntimeValueAsync(IDbConnection dbConnection, string projectId, IEnumerable<AssetRuntimeTrigger> assetRuntimeTriggers, IEnumerable<AssetAttribute> assetAttributes, DateTime timestamp)
        {
            // get runtime value
            var runtimeAttributeValues = new List<(Guid AssetId, Guid AttributeId, string DataType, object Value)>();
            var runtimeAttributes = (from attribute in assetAttributes
                                     join trigger in assetRuntimeTriggers on new { attribute.AssetId, attribute.AttributeId, attribute.TriggerAssetId, attribute.TriggerAttributeId } equals new { trigger.AssetId, trigger.AttributeId, trigger.TriggerAssetId, trigger.TriggerAttributeId }
                                     where attribute.AttributeType == AttributeTypeConstants.TYPE_RUNTIME && attribute.EnabledExpression == true
                                     select attribute).Distinct();
            _logger.LogDebug($"{nameof(CalculateRuntimeAttributeService)} - {nameof(GetRuntimeValueAsync)} - runtimeAttributes = {runtimeAttributes.ToJson()}");

            var dictionary = await GetDictionaryAsync(assetAttributes, dbConnection, projectId, assetRuntimeTriggers);

            foreach (var attribute in runtimeAttributes)
            {
                var attributeFailedHashField = CacheKey.PROCESSING_FAILED_HASH_FIELD.GetCacheKey(attribute.AssetId, attribute.AttributeId);
                var attributeFailedHashKey = CacheKey.PROCESSING_FAILED_HASH_KEY.GetCacheKey(projectId);

                try
                {
                    string attributeFailedHit = await _cache.GetHashByKeyInStringAsync(attributeFailedHashKey, attributeFailedHashField);
                    if (!string.IsNullOrEmpty(attributeFailedHit))
                    {
                        _logger.LogDebug("Attribute failed - from cache. System will ignore until user modified the value");
                        continue;
                    }

                    _logger.LogDebug($"{nameof(CalculateRuntimeAttributeService)} - {nameof(GetRuntimeValueAsync)} attribute id - {attribute.AttributeId} - dictionary = {dictionary.ToJson()}");
                    var value = _dynamicResolver.ResolveInstance("return true;", attribute.Expression).OnApply(dictionary);
                    dictionary[attribute.AttributeId.ToString()] = value;
                    _logger.LogDebug($"{nameof(CalculateRuntimeAttributeService)} - {nameof(GetRuntimeValueAsync)} attribute id - {attribute.AttributeId} - value = {value.ToJson()}");
                    runtimeAttributeValues.Add((attribute.AssetId, attribute.AttributeId, attribute.DataType, value));
                }
                catch (System.Exception exc)
                {
                    await _cache.SetHashByKeyAsync(attributeFailedHashKey, attributeFailedHashField, exc.Message, TimeSpan.FromDays(1));
                    _logger.LogError(exc, exc.Message);
                }
            }

            var assetInfos = new List<AssetInformation>();
            foreach (var assetId in runtimeAttributes.Select(x => x.AssetId).Distinct())
            {
                var assetInfo = await _readOnlyAssetRepository.GetAssetInformationsAsync(projectId, assetId);
                assetInfos.Add(assetInfo);
            }

            _logger.LogDebug($"{nameof(CalculateRuntimeAttributeService)} - {nameof(GetRuntimeValueAsync)} runtimeAttributeValues = {runtimeAttributeValues.ToJson()}");
            var values = runtimeAttributeValues.Select(x => new RuntimeValueObject
            {
                Timestamp = timestamp,
                AssetId = x.AssetId,
                AttributeId = x.AttributeId,
                Value = ParseValueToStore(x.DataType, x.Value),
                DataType = x.DataType,
                RetentionDays = assetInfos.First(i => i.AssetId == x.AssetId).RetentionDays
            });

            return values;
        }

        private async Task<Dictionary<string, object>> GetDictionaryAsync(
            IEnumerable<AssetAttribute> assetAttributes,
            IDbConnection dbConnection,
            string projectId,
            IEnumerable<AssetRuntimeTrigger> assetRuntimeTriggers)
        {
            var aliasMapping = await GetListAliasMappingAsync(projectId, assetAttributes, assetRuntimeTriggers, dbConnection);
            var attributeIds = assetAttributes.Where(x => x.AttributeType != AttributeTypeConstants.TYPE_ALIAS)
                                              .Select(x => x.AttributeId)
                                              .Union(aliasMapping
                                              .Select(x => x.Item1))
                                              .Distinct()
                                              .ToList();
            var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
            var snapshots = await retryStrategy.ExecuteAsync(async () =>
                await dbConnection.QueryAsync<AttributeSnapshot>(@"
                    select attribute_id as AttributeId, value as Value, _ts as Timestamp, data_type as DataType 
                    from v_asset_attribute_snapshots 
                    where attribute_id = ANY(@AttributeIds)",
                    new { AttributeIds = attributeIds },
                    commandTimeout: 2)
            );

            var attributeSnapshots = snapshots.SelectMany(x =>
            {
                object value = ParseValue(x.DataType, x.Value);
                Guid attributeId = x.AttributeId;
                return aliasMapping.Where(x => x.Item1 == attributeId).Select(mapping => (mapping.Item2, Value: value, x.Timestamp));
            });

            var dictionary = new Dictionary<string, object>();
            foreach (var attribute in assetAttributes)
            {
                var snapshot = snapshots.Where(x => x.AttributeId == attribute.AttributeId).Select(t => (t.Value, t.Timestamp)).FirstOrDefault();
                if (snapshot.Value != null &&
                    (string.Equals(attribute.AttributeType, AttributeTypeConstants.TYPE_STATIC, StringComparison.InvariantCultureIgnoreCase) || snapshot.Timestamp.HasValue))
                {
                    object value = ParseValue(attribute.DataType, snapshot.Value);
                    dictionary[attribute.AttributeId.ToString()] = value;
                }
                else
                {
                    var aliasSnapshot = attributeSnapshots.Where(x => x.Item1 == attribute.AttributeId).Select(t => (t.Value, t.Timestamp)).FirstOrDefault();
                    if (aliasSnapshot.Value != null && aliasSnapshot.Timestamp.HasValue)
                    {
                        dictionary[attribute.AttributeId.ToString()] = aliasSnapshot.Value;
                    }
                    else
                    {
                        // cannot find the snapshot for this attribute, can cause the issue with runtime attribute
                        _logger.LogError($"Cannot find the snapshot for this asset attribute {attribute.AssetId}/{attribute.AttributeId}");
                    }
                }
            }

            return dictionary;
        }

        private async Task<List<(Guid, Guid)>> GetListAliasMappingAsync(string projectId, IEnumerable<AssetAttribute> assetAttributes, IEnumerable<AssetRuntimeTrigger> assetRuntimeTriggers, IDbConnection dbConnection)
        {
            var aliasMapping = new List<(Guid, Guid)>();

            foreach (var alias in assetAttributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS))
            {
                var aliasFailedHashField = CacheKey.PROCESSING_FAILED_HASH_FIELD.GetCacheKey(alias.AssetId, alias.AttributeId);
                var aliasFailedHashKey = CacheKey.PROCESSING_FAILED_HASH_KEY.GetCacheKey(projectId);

                var aliasHashField = CacheKey.PROCESSING_ASSET_HASH_FIELD.GetCacheKey(alias.AssetId, alias.AttributeId);
                var aliasHashKey = CacheKey.PROCESSING_ASSET_HASH_KEY.GetCacheKey(projectId);

                try
                {
                    string aliasFailedHit = await _cache.GetHashByKeyInStringAsync(aliasFailedHashKey, aliasFailedHashField);
                    if (!string.IsNullOrEmpty(aliasFailedHit))
                    {
                        _logger.LogDebug("Alias failed - from cache. System will ignore until user modified the value");
                        continue;
                    }

                    string targetAliasAttributeIdString = await _cache.GetHashByKeyInStringAsync(aliasHashKey, aliasHashField);
                    if (targetAliasAttributeIdString == null)
                    {
                        var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                        var targetAliasAttributeId = await retryStrategy.ExecuteAsync(async () =>
                            await dbConnection.QuerySingleOrDefaultAsync<Guid>(@"
                                select attribute_id 
                                from find_root_alias_asset_attribute(@AliasAttributeId) 
                                order by alias_level desc limit 1",
                                new { AliasAttributeId = alias.AttributeId },
                                commandTimeout: 2)
                        );

                        targetAliasAttributeIdString = targetAliasAttributeId.ToString();
                        await _cache.SetHashByKeyAsync(aliasHashKey, aliasHashField, targetAliasAttributeIdString);
                    }
                    else
                    {
                        // nothing change. no need to update
                        _logger.LogDebug("GetListAliasMappingAsync - Cache hit, no change");
                    }

                    aliasMapping.Add((Guid.Parse(targetAliasAttributeIdString), alias.AttributeId));
                }
                catch (Exception exc)
                {
                    await _cache.SetHashByKeyAsync(aliasFailedHashKey, aliasFailedHashField, exc.Message, TimeSpan.FromDays(1));
                    _logger.LogError(exc, exc.Message);
                }
            }

            return aliasMapping;
        }

        private async Task<IEnumerable<AssetAttribute>> GetAssetAttributesAsync(string projectId, IEnumerable<AssetRuntimeTrigger> assetRuntimeTriggers, IDbConnection dbConnection)
        {
            if (assetRuntimeTriggers == null || !assetRuntimeTriggers.Any())
            {
                return Enumerable.Empty<AssetAttribute>();
            }

            var assetIds = assetRuntimeTriggers.Select(x => x.AssetId).ToList();
            var assetAttributeHashField = CacheKey.PROCESSING_ASSET_IDS_HASH_FIELD.GetCacheKey(string.Join(',', assetIds), "attributes_trigger");
            var assetAttributeHashKey = CacheKey.PROCESSING_ASSET_IDS_HASH_KEY.GetCacheKey(projectId);

            var assetAttributes = await _cache.GetHashByKeyAsync<IEnumerable<AssetAttribute>>(assetAttributeHashKey, assetAttributeHashField);

            if (assetAttributes == null)
            {
                var retryStrategy = RetryHelper.GetDbTimeoutRetryStrategyAsync(_logger);
                assetAttributes = await retryStrategy.ExecuteAsync(async () =>
                    await dbConnection.QueryAsync<AssetAttribute>(@"select
                                                                    aa.asset_id as AssetId
                                                                    , aa.attribute_id as AttributeId
                                                                    , aa.data_type as DataType
                                                                    , aa.attribute_type as AttributeType
                                                                    , aa.expression_compile as Expression
                                                                    , aa.enabled_expression as EnabledExpression
                                                                    , aart.trigger_asset_id as TriggerAssetId
                                                                    , aart.trigger_attribute_id as TriggerAttributeId
                                                                    from v_asset_attributes aa
                                                                    left join asset_attribute_runtime_triggers aart on aart.asset_id = aa.asset_id and aart.attribute_id = aa.attribute_id
                                                                    where aa.asset_id = ANY(@AssetIds)
                                                                    order by aa.created_utc, aa.sequential_number", new { AssetIds = assetIds.ToArray() })
                );

                await _cache.SetHashByKeyAsync(assetAttributeHashKey, assetAttributeHashField, assetAttributes);
            }
            else
            {
                // nothing change. no need to update
                _logger.LogDebug($"{nameof(GetAssetAttributesAsync)} - Cache hit, no change");
            }

            return assetAttributes;
        }

        private async Task<(Dictionary<string, IEnumerable<AssetAttribute>>, Dictionary<string, IEnumerable<AssetRuntimeTrigger>>, Dictionary<string, IEnumerable<RuntimeValueObject>>, Dictionary<string, long>)> GetAllDataAssetRuntime(IDictionary<string, object> metricDict, IEnumerable<DeviceInformation> listDeviceInformation, string projectId)
        {
            var dataAssetAttributes = new Dictionary<string, IEnumerable<AssetAttribute>>();
            var dataAssetRuntimeTriggers = new Dictionary<string, IEnumerable<AssetRuntimeTrigger>>();
            var dataRuntimeValues = new Dictionary<string, IEnumerable<RuntimeValueObject>>();
            var dataUnixTimestamps = new Dictionary<string, long>();

            using var dbConnection = GetReadOnlyDbConnection(projectId);
            foreach (var deviceInformation in listDeviceInformation)
            {
                var timestampKeys = deviceInformation.Metrics.Where(x => x.MetricType == TemplateKeyTypes.TIMESTAMP).Select(x => x.MetricKey);
                var timestamp = (from ts in timestampKeys
                                 join metric in metricDict on ts.ToLowerInvariant() equals metric.Key.ToLowerInvariant()
                                 select metric.Value.ToString()
                    ).FirstOrDefault();
                var deviceTimestamp = DateTimeExtensions.CutOffNanoseconds(timestamp.CutOffFloatingPointPlace().UnixTimeStampToDateTime());

                var deviceId = deviceInformation.DeviceId;
                var assetRuntimeTriggers = await _deviceRepository.GetAssetTriggerAsync(projectId, deviceId);
                _logger.LogDebug($"{nameof(CalculateRuntimeAttributeService)} - {nameof(GetAllDataAssetRuntime)} - assetRuntimeTriggers = {assetRuntimeTriggers.ToJson()}");
                if (!metricDict.ContainsKey(MetricPayload.INTEGRATION_ID))
                {
                    var snapshotMetrics = GetTotalSnapshotMetrics(metricDict, deviceInformation);
                    var metrics = snapshotMetrics.Select(x => x.MetricKey).Distinct().ToArray();
                    assetRuntimeTriggers = assetRuntimeTriggers.Where(ra => metrics.Contains(ra.MetricKey) || ra.MetricKey == null);

                    var lastestSnapshot = snapshotMetrics.First();

                    deviceTimestamp = lastestSnapshot.Timestamp;

                    // save UnixTimestamp for notify
                    dataUnixTimestamps.Add(deviceId, lastestSnapshot.UnixTimestamp);
                }

                if (assetRuntimeTriggers.Any())
                {
                    //save asset runtime trigger to dictionary
                    dataAssetRuntimeTriggers.Add(deviceId, assetRuntimeTriggers);

                    var assetAttributes = await GetAssetAttributesAsync(projectId, assetRuntimeTriggers, dbConnection);
                    _logger.LogDebug($"{nameof(CalculateRuntimeAttributeService)} - {nameof(GetAllDataAssetRuntime)} - assetAttributes = {assetAttributes.ToJson()}");

                    //save runtime value to dictionary
                    var values = await GetRuntimeValueAsync(dbConnection, projectId, assetRuntimeTriggers, assetAttributes, deviceTimestamp);
                    dataRuntimeValues.Add(deviceId, values);

                    // save assets attributes to dictionary
                    dataAssetAttributes.Add(deviceId, assetAttributes);
                }
            }

            dbConnection.Close();

            return (dataAssetAttributes, dataAssetRuntimeTriggers, dataRuntimeValues, dataUnixTimestamps);
        }
    }
}