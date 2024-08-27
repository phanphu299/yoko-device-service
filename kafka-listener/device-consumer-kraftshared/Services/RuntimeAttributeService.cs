using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using Device.Consumer.KraftShared.Constant;
using Device.Consumer.KraftShared.Service.Abstraction;
using Device.Consumer.KraftShared.Service.Model;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.Interceptor.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Device.Consumer.KraftShared.Model;
using Microsoft.Extensions.Caching.Memory;
using Device.Consumer.KraftShared.Repositories.Abstraction.ReadOnly;
using Device.Consumer.KraftShared.Services;
using Device.Consumer.KraftShared.Repositories.Abstraction;
using Device.Consumer.KraftShared.Models.MetricModel;
using Device.Consumer.KraftShared.Extensions;
namespace Device.Consumer.KraftShared.Service
{
    public class RuntimeAttributeService : IRuntimeAttributeService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IReadOnlyDbConnectionFactory _readOnlyDbConnectionFactory;
        private readonly IDynamicResolver _dynamicResolver;
        private readonly ILoggerAdapter<RuntimeAttributeService> _logger;
        private readonly ICache _cache;
        private readonly IMemoryCache _memoryCache;
        private readonly IReadOnlyAssetRepository _readOnlyAssetRepository;
        private readonly string _podName;
        private readonly IConfiguration _configuration;

        public RuntimeAttributeService(
            IDbConnectionFactory dbConnectionFactory, 
            IReadOnlyDbConnectionFactory readOnlyDbConnectionFactory, 
            IDynamicResolver dynamicResover, 
            ILoggerAdapter<RuntimeAttributeService> logger, 
            ICache cache, 
            IConfiguration configuration, 
            IMemoryCache memoryCache, 
            IReadOnlyAssetRepository readOnlyAssetRepository
            )
        {
            _dbConnectionFactory = dbConnectionFactory;
            _readOnlyDbConnectionFactory = readOnlyDbConnectionFactory;
            _dynamicResolver = dynamicResover;
            _logger = logger;
            _cache = cache;
            _configuration = configuration;
            _memoryCache = memoryCache;
            _readOnlyAssetRepository = readOnlyAssetRepository;
            _podName = configuration["PodName"] ?? "device-function";
        }
        public async Task<IEnumerable<Guid>> CalculateRuntimeValueAsync(string projectId, DateTime timestamp, IEnumerable<AssetRuntimeTrigger> assetRuntimeTriggers)
        {
            if (assetRuntimeTriggers == null || !assetRuntimeTriggers.Any())
            {
                return Array.Empty<Guid>();
            }
            var assetIds = assetRuntimeTriggers.Select(x => x.AssetId).ToList();
            try
            {
                var assetAttributeKey = $"{_podName}_{projectId}_processing_assets_ids_{string.Join(',', assetIds)}_attributes_trigger";
                var cacheHit = await _cache.GetStringAsync(assetAttributeKey, RedisConstants.PROCESSING_DEFAULT_DATABASE);
                if (string.IsNullOrEmpty(cacheHit))
                {
                    // reset memory cache
                    _memoryCache.Set<IEnumerable<AssetAttribute>>(assetAttributeKey, null);
                }
                var assetAttributes = _memoryCache.Get<IEnumerable<AssetAttribute>>(assetAttributeKey);
                if (assetAttributes == null)
                {
                    using (var dbConnection = _readOnlyDbConnectionFactory.CreateConnection(projectId))
                    {
                        assetAttributes = await dbConnection.QueryAsync<AssetAttribute>(@"select 
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
                                                                                order by aa.created_utc, aa.sequential_number", new { AssetIds = assetIds.ToArray() });
                        dbConnection.Close();
                    }
                    _memoryCache.Set(assetAttributeKey, assetAttributes);
                    await _cache.StoreAsync(assetAttributeKey, "1", RedisConstants.PROCESSING_DEFAULT_DATABASE);
                }
                var aliasMapping = new List<(Guid, Guid)>();
                foreach (var alias in assetAttributes.Where(x => x.AttributeType == AttributeTypeConstants.TYPE_ALIAS))
                {
                    var aliasFailedKey = $"{projectId}_processing_asset_{alias.AssetId}_attribute_{alias.AttributeId}_failed";
                    var aliasKey = $"{_podName}_{projectId}_processing_asset_{alias.AssetId}_attribute_{alias.AttributeId}";
                    try
                    {
                        var aliasFailedHit = await _cache.GetStringAsync(aliasFailedKey, RedisConstants.PROCESSING_DEFAULT_DATABASE);
                        if (!string.IsNullOrEmpty(aliasFailedHit))
                        {
                            // _logger.LogDebug("Alias failed - from cache. System will ignore until user modified the value");
                            continue;
                        }
                        cacheHit = await _cache.GetStringAsync(aliasKey, RedisConstants.PROCESSING_DEFAULT_DATABASE);
                        if (string.IsNullOrEmpty(cacheHit))
                        {
                            // reset the cache
                            _memoryCache.Set<string>(aliasKey, null);
                        }
                        var targetAliasAttributeIdString = _memoryCache.Get<string>(aliasKey);
                        if (targetAliasAttributeIdString == null)
                        {
                            using (var dbConnection = _readOnlyDbConnectionFactory.CreateConnection(projectId))
                            {
                                var targetAliasAttributeId = await dbConnection.QuerySingleOrDefaultAsync<Guid>("select attribute_id from find_root_alias_asset_attribute(@AliasAttributeId) order by alias_level desc limit 1", new { AliasAttributeId = alias.AttributeId }, commandTimeout: 2);
                                dbConnection.Close();
                                targetAliasAttributeIdString = targetAliasAttributeId.ToString();
                            }
                            _memoryCache.Set(aliasKey, targetAliasAttributeIdString);
                            await _cache.StoreAsync(aliasKey, "1", RedisConstants.PROCESSING_DEFAULT_DATABASE);
                        }
                        aliasMapping.Add((Guid.Parse(targetAliasAttributeIdString), alias.AttributeId));
                    }
                    catch (System.Exception exc)
                    {
                        await _cache.StoreAsync(aliasFailedKey, exc.Message, TimeSpan.FromDays(1), RedisConstants.PROCESSING_DEFAULT_DATABASE);
                        _logger.LogError("Ingestion_Error CalculateRuntimeValueAsync.1 projectId: {projectId} - ex: {ex}" , projectId, exc);
                    }
                }
                var attributeIds = assetAttributes.Where(x => x.AttributeType != AttributeTypeConstants.TYPE_ALIAS).Select(x => x.AttributeId).Union(aliasMapping.Select(x => x.Item1)).Distinct().ToList();
                using (var dbConnection = _dbConnectionFactory.CreateConnection(projectId))
                {
                    var snapshots = await dbConnection.QueryAsync<AttributeSnapshot>("select attribute_id as AttributeId, value as Value, _ts as Timestamp, data_type as DataType from v_asset_attribute_snapshots where attribute_id = ANY(@AttributeIds)", new { AttributeIds = attributeIds });
                    var attributeSnapshots = snapshots.SelectMany(x =>
                    {
                        object value = DeviceService.ParseValue(x.DataType, x.Value);
                        Guid attributeId = x.AttributeId;
                        return aliasMapping.Where(x => x.Item1 == attributeId).Select(mapping => (mapping.Item2, Value: value, x.Timestamp));
                    });
                    var dictionary = new Dictionary<string, object>();
                    foreach (var attribute in assetAttributes)
                    {
                        var snapshot = snapshots.Where(x => x.AttributeId == attribute.AttributeId).Select(t => (t.Value, t.Timestamp)).FirstOrDefault();
                        if (snapshot.Value != null
                            && (string.Equals(attribute.AttributeType, AttributeTypeConstants.TYPE_STATIC, StringComparison.InvariantCultureIgnoreCase)
                                || snapshot.Timestamp.HasValue
                            ))
                        {
                            object value = DeviceService.ParseValue(attribute.DataType, snapshot.Value);
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
                                _logger.LogError($"Ingestion_Error Cannot find the snapshot for this asset attribute {attribute.AssetId}/{attribute.AttributeId}");
                            }
                        }
                    }
                    // get runtime value            
                    var runtimeAttributeValues = new List<(Guid AssetId, Guid AttributeId, string DataType, object Value)>();
                    var runtimeAttributes = (from attribute in assetAttributes
                                             join trigger in assetRuntimeTriggers on new { attribute.AssetId, attribute.AttributeId, attribute.TriggerAssetId, attribute.TriggerAttributeId } equals new { trigger.AssetId, trigger.AttributeId, trigger.TriggerAssetId, trigger.TriggerAttributeId }
                                             where attribute.AttributeType == AttributeTypeConstants.TYPE_RUNTIME && attribute.EnabledExpression == true
                                             select attribute).Distinct();

                    foreach (var attribute in runtimeAttributes)
                    {
                        var attributeFailedKey = $"{projectId}_processing_asset_{attribute.AssetId}_attribute_{attribute.AttributeId}_failed";
                        try
                        {
                            var attributeFailedHit = await _cache.GetStringAsync(attributeFailedKey, RedisConstants.PROCESSING_DEFAULT_DATABASE);
                            if (!string.IsNullOrEmpty(attributeFailedHit))
                            {
                                _logger.LogDebug("Attribute failed - from cache. System will ignore until user modified the value");
                                continue;
                            }
                            var value = _dynamicResolver.ResolveInstance("return true;", attribute.Expression).OnApply(dictionary);
                            dictionary[attribute.AttributeId.ToString()] = value;
                            runtimeAttributeValues.Add((attribute.AssetId, attribute.AttributeId, attribute.DataType, value));
                        }
                        catch (System.Exception exc)
                        {
                            await _cache.StoreAsync(attributeFailedKey, exc.Message, TimeSpan.FromDays(1), RedisConstants.PROCESSING_DEFAULT_DATABASE);
                            _logger.LogError("Ingestion_Error CalculateRuntimeValueAsync.2 projectId: {projectId} - ex: {ex}" , projectId, exc);
                        }
                    }
                    // var assetInfoTasks = runtimeAttributeValues.Select(x => _assetRepository.GetAssetInformationsAsync(dbConnection, projectId, x.AssetId));
                    // var assetInfos = await Task.WhenAll(assetInfoTasks);
                    var assetInfos = new List<AssetInformation>();
                    foreach (var assetId in runtimeAttributes.Select(x => x.AssetId).Distinct())
                    {
                        var assetInfo = await _readOnlyAssetRepository.GetAssetInformationsAsync(projectId, assetId);
                        assetInfos.Add(assetInfo);
                    }
                    var values = runtimeAttributeValues.Select(x => new
                    {
                        Timestamp = timestamp,
                        AssetId = x.AssetId,
                        AttributeId = x.AttributeId,
                        Value = DeviceService.ParseValueToStore(x.DataType, x.Value),
                        DataType = x.DataType,
                        RetentionDays = assetInfos.First(i => i.AssetId == x.AssetId).RetentionDays
                    });
                    if (values.Any())
                    {
                        await dbConnection.ExecuteAsync($@"INSERT INTO asset_attribute_runtime_snapshots(_ts, asset_id, asset_attribute_id, value) 
                                                VALUES(@Timestamp, @AssetId, @AttributeId, @Value)
                                                ON CONFLICT (asset_id, asset_attribute_id) 
                                                DO UPDATE SET _ts = EXCLUDED._ts, value = EXCLUDED.value WHERE asset_attribute_runtime_snapshots._ts < EXCLUDED._ts;
                                                ", values);
                    }

                    var numericValues = values.Where(x => DataTypeExtensions.IsNumericTypeSeries(x.DataType));
                    if (numericValues.Any())
                    {
                        await dbConnection.ExecuteAsync($@" INSERT INTO asset_attribute_runtime_series(_ts, asset_id, asset_attribute_id, value, retention_days)
                                                VALUES (@Timestamp, @AssetId, @AttributeId, @Value, @RetentionDays);
                                                ", numericValues);
                    }

                    var textValues = values.Where(x => DataTypeExtensions.IsTextTypeSeries(x.DataType)).Select(x => new
                    {
                        Timestamp = timestamp,
                        AssetId = x.AssetId,
                        AttributeId = x.AttributeId,
                        Value = x.Value.ToString(), // should be a string
                        DataType = x.DataType,
                        RetentionDays = assetInfos.First(i => i.AssetId == x.AssetId).RetentionDays
                    });
                    if (textValues.Any())
                    {
                        await dbConnection.ExecuteAsync($@" INSERT INTO asset_attribute_runtime_series_text(_ts, asset_id, asset_attribute_id, value, retention_days)
                                                VALUES (@Timestamp, @AssetId, @AttributeId, @Value, @RetentionDays);
                                                ", textValues);
                    }
                    dbConnection.Close();
                    return numericValues.Select(x => x.AssetId).Union(textValues.Select(x => x.AssetId)).Distinct();
                }
            }
            catch (System.Exception exc)
            {
                _logger.LogError("Ingestion_Error CalculateRuntimeValueAsync.3 projectId: {projectId} - ex: {ex}" , projectId, exc);
            }
            return Array.Empty<Guid>();
        }
    }
    //public class AttributeSnapshot
    //{
    //    public Guid AttributeId { get; set; }
    //    public string Value { get; set; }
    //    public string DataType { get; set; }
    //    public DateTime? Timestamp { get; set; }
    //}
}
