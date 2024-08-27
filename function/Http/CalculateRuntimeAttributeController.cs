using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using AHI.Infrastructure.Cache.Abstraction;
using System.Collections.Generic;
using Npgsql;
using Dapper;
using AHI.Infrastructure.MultiTenancy.Extension;
using AHI.Device.Function.Constant;
using Function.Extension;
using AHI.Infrastructure.Security.Helper;

namespace Function.Http
{
    public class CalculateRuntimeAttributeController
    {
        private readonly IRuntimeAttributeService _runtimeAttributeService;
        private readonly IConfiguration _configuration;
        private readonly ITenantContext _tenantContext;
        private readonly IAssetNotificationService _notificationService;
        private readonly ICache _cache;

        public CalculateRuntimeAttributeController(IRuntimeAttributeService runtimeAttributeService
                                                    , IConfiguration configuration
                                                    , IAssetNotificationService notificationService
                                                    , ITenantContext tenantContext
                                                    , ICache cache
            )
        {
            _runtimeAttributeService = runtimeAttributeService;
            _configuration = configuration;
            _notificationService = notificationService;
            _tenantContext = tenantContext;
            _cache = cache;
        }

        [FunctionName("CalculateRuntimeBaseTriggerAttributeController")]
        public async Task<IActionResult> CalculateRuntimeBaseTriggerAttributeAsync(
                [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fnc/dev/devices/calculate/trigger")] HttpRequestMessage req
        )
        {
            ClaimsPrincipal principal = await SecurityHelper.ValidateTokenAsync(req.Headers.Authorization);
            if (principal == null)
            {
                return new UnauthorizedResult();
            }

            var payload = await req.Content.ReadAsByteArrayAsync();
            var message = payload.Deserialize<AttributeAttributeTriggerRequest>();
            if (!message.Assets.Any())
            {
                return new OkResult();
            }
            _tenantContext.RetrieveFromString(message.TenantId, message.SubscriptionId, message.ProjectId);

            // find the trigger attribute
            var assetAttributeRelevantHashField = CacheKey.PROCESSING_ASSET_IDS_HASH_FIELD.GetCacheKey(string.Join("_", message.Assets.Select(x => x.Id)), "asset_related_runtime_trigger");
            var assetAttributeRelevantHashKey = CacheKey.PROCESSING_ASSET_IDS_HASH_KEY.GetCacheKey(message.ProjectId);

            var runtimeAssets = await _cache.GetHashByKeyAsync<IEnumerable<AssetRuntimeTrigger>>(assetAttributeRelevantHashKey, assetAttributeRelevantHashField);

            if (runtimeAssets == null)
            {
                var connectionString = _configuration["ConnectionStrings:Default"].BuildConnectionString(_configuration, _tenantContext.ProjectId);
                using (var dbConnection = new NpgsqlConnection(connectionString))
                {
                    runtimeAssets = await dbConnection.QueryAsync<AssetRuntimeTrigger>(@"
                        select
                            rrt.asset_id as AssetId ,
                            rrt.attribute_id  as AttributeId,
                            rrt.trigger_asset_id as TriggerAssetId,
                            rrt.trigger_attribute_id as TriggerAttributeId
                        from
                            find_all_attribute_trigger_relation(@AssetAttributeIds) rrt
                    ", new { AssetAttributeIds = message.Assets.Select(x => x.Id).ToArray() });

                    await dbConnection.CloseAsync();
                    await _cache.SetHashByKeyAsync(assetAttributeRelevantHashKey, assetAttributeRelevantHashField, runtimeAssets);
                }
            }

            var notifyAssetIds = message.Assets.Select(x => x.AssetId).ToList();
            if (runtimeAssets.Any())
            {
                var runtimeAffectedAssetIds = await _runtimeAttributeService.CalculateRuntimeValueAsync(_tenantContext.ProjectId, DateTime.UtcNow, runtimeAssets);
                notifyAssetIds.AddRange(runtimeAffectedAssetIds);
            }
            var notificationTasks = notifyAssetIds.Distinct().Select(assetId =>
            {
                var notificationMessage = new AssetNotificationMessage(AHI.Device.Function.Constant.NotificationType.ASSET, assetId);
                return _notificationService.NotifyAssetAsync(notificationMessage);
            });
            // FE need to load the snapshot when receiving this message.
            await Task.WhenAll(notificationTasks);
            return new OkResult();
        }

        [FunctionName("CalculateRuntimeAttributeController")]
        public async Task<IActionResult> CalculateRuntimeAttributeAsync(
                [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "fnc/dev/devices/calculate")] HttpRequestMessage req
        )
        {
            ClaimsPrincipal principal = await SecurityHelper.ValidateTokenAsync(req.Headers.Authorization);
            if (principal == null)
            {
                return new UnauthorizedResult();
            }

            var payload = await req.Content.ReadAsByteArrayAsync();
            var message = payload.Deserialize<AttributeAttributeTriggerRequest>();
            if (!message.Assets.Any())
            {
                return new OkResult();
            }
            _tenantContext.RetrieveFromString(message.TenantId, message.SubscriptionId, message.ProjectId);
            IEnumerable<AssetRuntimeTrigger> runtimeAssets = null;
            var connectionString = _configuration["ConnectionStrings:Default"].BuildConnectionString(_configuration, _tenantContext.ProjectId);
            using (var dbConnection = new NpgsqlConnection(connectionString))
            {
                runtimeAssets = await dbConnection.QueryAsync<AssetRuntimeTrigger>(@"
                        select
                            rrt.asset_id as AssetId ,
                            rrt.attribute_id  as AttributeId,
                            rrt.trigger_asset_id as TriggerAssetId,
                            rrt.trigger_attribute_id as TriggerAttributeId
                        from
                            asset_attribute_runtime_triggers rrt 
                        where rrt.attribute_id = ANY(@AssetAttributeIds) and rrt.is_selected = true
                    ", new { AssetAttributeIds = message.Assets.Select(x => x.Id).ToArray() });
                dbConnection.Close();
            }
            if (runtimeAssets.Any())
            {
                _ = await _runtimeAttributeService.CalculateRuntimeValueAsync(_tenantContext.ProjectId, DateTime.UtcNow, runtimeAssets);
            }
            return new OkResult();
        }
    }
}