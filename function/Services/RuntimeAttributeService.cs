using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Configuration;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Service.Abstraction;
using AHI.Device.Function.Service.Model;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.Interceptor.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Device.Function.Model;
using Microsoft.Extensions.Caching.Memory;
using AHI.Infrastructure.Repository.Abstraction.ReadOnly;
using AHI.Device.Function.Services;
using AHI.Infrastructure.Repository.Abstraction;
using Function.Extension;

namespace AHI.Device.Function.Service
{
    public class RuntimeAttributeService : IRuntimeAttributeService
    {
        private readonly IReadOnlyDbConnectionFactory _readOnlyDbConnectionFactory;
        private readonly IDynamicResolver _dynamicResolver;
        private readonly ILoggerAdapter<RuntimeAttributeService> _logger;
        private readonly ICache _cache;
        private readonly IReadOnlyAssetRepository _readOnlyAssetRepository;

        public RuntimeAttributeService(
            IDbConnectionFactory dbConnectionFactory,
            IReadOnlyDbConnectionFactory readOnlyDbConnectionFactory,
            IDynamicResolver dynamicResover,
            IDataProcessor dataProcessor,
            ILoggerAdapter<RuntimeAttributeService> logger,
            ICache cache,
            IConfiguration configuration,
            IReadOnlyAssetRepository readOnlyAssetRepository
            )
        {
            _readOnlyDbConnectionFactory = readOnlyDbConnectionFactory;
            _dynamicResolver = dynamicResover;
            _logger = logger;
            _cache = cache;
            _readOnlyAssetRepository = readOnlyAssetRepository;
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
                var assetAttributeHashField = CacheKey.PROCESSING_ASSET_IDS_HASH_FIELD.GetCacheKey(string.Join(',', assetIds), "attributes_trigger");
                var assetAttributeHashKey = CacheKey.PROCESSING_ASSET_IDS_HASH_KEY.GetCacheKey(projectId);

                var assetAttributes = await _cache.GetHashByKeyAsync<IEnumerable<AssetAttribute>>(assetAttributeHashKey, assetAttributeHashField);

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
                        await dbConnection.CloseAsync();
                    }

                    await _cache.SetHashByKeyAsync(assetAttributeHashKey, assetAttributeHashField, assetAttributes);
                }

                var aliasMapping = await GetMappingAliasAsync(assetAttributes, projectId);
                using (var dbConnection = _readOnlyDbConnectionFactory.CreateConnection(projectId))
                {
                    var dictionary = await GetSnapshotValueAsync(assetAttributes, aliasMapping, projectId, dbConnection);
                    // get runtime value            
                    var runtimeAttributes = (from attribute in assetAttributes
                                             join trigger in assetRuntimeTriggers on new { attribute.AssetId, attribute.AttributeId, attribute.TriggerAssetId, attribute.TriggerAttributeId } equals new { trigger.AssetId, trigger.AttributeId, trigger.TriggerAssetId, trigger.TriggerAttributeId }
                                             where attribute.AttributeType == AttributeTypeConstants.TYPE_RUNTIME && attribute.EnabledExpression == true
                                             select attribute).Distinct();
                    var runtimeAttributeValues = await GetRuntimeValueAsync(runtimeAttributes, dictionary, projectId);
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

                    await dbConnection.CloseAsync();
                    return numericValues.Select(x => x.AssetId).Union(textValues.Select(x => x.AssetId)).Distinct();
                }
            }
            catch (System.Exception exc)
            {
                _logger.LogError(exc, exc.Message);
            }

            return Array.Empty<Guid>();
        }

        private async Task<IEnumerable<(Guid, Guid)>> GetMappingAliasAsync(IEnumerable<AssetAttribute> assetAttributes, string projectId)
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
                        using (var dbConnection = _readOnlyDbConnectionFactory.CreateConnection(projectId))
                        {
                            var targetAliasAttributeId = await dbConnection.QuerySingleOrDefaultAsync<Guid>("select attribute_id from find_root_alias_asset_attribute(@AliasAttributeId) order by alias_level desc limit 1", new { AliasAttributeId = alias.AttributeId }, commandTimeout: 2);
                            await dbConnection.CloseAsync();
                            targetAliasAttributeIdString = targetAliasAttributeId.ToString();
                        }

                        await _cache.SetHashByKeyAsync(aliasHashKey, aliasHashField, targetAliasAttributeIdString);
                    }

                    aliasMapping.Add((Guid.Parse(targetAliasAttributeIdString), alias.AttributeId));
                }
                catch (System.Exception exc)
                {
                    await _cache.SetHashByKeyAsync(aliasFailedHashKey, aliasFailedHashField, exc.Message, TimeSpan.FromDays(1));
                    _logger.LogError(exc.Message, exc);
                }
            }
            return aliasMapping;
        }

        private async Task<IDictionary<string, object>> GetSnapshotValueAsync(IEnumerable<AssetAttribute> assetAttributes, IEnumerable<(Guid, Guid)> aliasMapping, string projectId, IDbConnection dbConnection)
        {
            var attributeIds = assetAttributes.Where(x => x.AttributeType != AttributeTypeConstants.TYPE_ALIAS).Select(x => x.AttributeId).Union(aliasMapping.Select(x => x.Item1)).Distinct().ToList();
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
                        _logger.LogError($"Cannot find the snapshot for this asset attribute {attribute.AssetId}/{attribute.AttributeId}");
                    }
                }
            }
            return dictionary;
        }

        private async Task<IEnumerable<(Guid AssetId, Guid AttributeId, string DataType, object Value)>> GetRuntimeValueAsync(IEnumerable<AssetAttribute> runtimeAttributes, IDictionary<string, object> dictionary, string projectId)
        {
            var runtimeAttributeValues = new List<(Guid AssetId, Guid AttributeId, string DataType, object Value)>();

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
                    var value = _dynamicResolver.ResolveInstance("return true;", attribute.Expression).OnApply(dictionary);
                    dictionary[attribute.AttributeId.ToString()] = value;
                    runtimeAttributeValues.Add((attribute.AssetId, attribute.AttributeId, attribute.DataType, value));
                }
                catch (System.Exception exc)
                {
                    await _cache.SetHashByKeyAsync(attributeFailedHashKey, attributeFailedHashField, exc.Message, TimeSpan.FromDays(1));
                    _logger.LogError(exc.Message, exc);
                }
            }

            return runtimeAttributeValues;
        }
    }
}