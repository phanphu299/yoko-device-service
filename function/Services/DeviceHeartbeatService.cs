using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AHI.Device.Function.Constant;
using AHI.Device.Function.Events;
using AHI.Device.Function.Model;
using AHI.Device.Function.Service.Abstraction;
using AHI.Infrastructure.Bus.ServiceBus.Abstraction;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using AHI.Infrastructure.SharedKernel.Extension;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using AHI.Infrastructure.Repository.Abstraction;
using Function.Extension;

namespace AHI.Device.Function.Service
{
    public class DeviceHeartbeatService : IDeviceHeartbeatService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly ILoggerAdapter<DeviceHeartbeatService> _logger;
        private readonly ICache _cache;
        private readonly IMemoryCache _memoryCache;
        private readonly IDomainEventDispatcher _domainEventDispatcher;
        private readonly ITenantContext _tenantContext;

        public DeviceHeartbeatService(IDbConnectionFactory dbConnectionFactory,
            ILoggerAdapter<DeviceHeartbeatService> logger,
            ICache cache,
            IDomainEventDispatcher domainEventDispatcher,
            ITenantContext tenantContext,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _cache = cache;
            _memoryCache = memoryCache;
            _dbConnectionFactory = dbConnectionFactory;
            _domainEventDispatcher = domainEventDispatcher;
            _tenantContext = tenantContext;
        }

        public async Task<IEnumerable<(string DeviceId, string TenantId, string SubscriptionId, string ProjectId)>> ProcessSignalQualityAsync(ProjectDto project)
        {
            _logger.LogDebug($"[ProcessSignalQualityAsync] - Start processing for Project {project.Id}...");

            try
            {
                var processingKey = $"ProcessSignalQualityAsync_{project.Id}";
                var exists = _memoryCache.Get(processingKey);
                if (exists != null)
                {
                    _logger.LogTrace($"[ProcessSignalQualityAsync][{project.Id}] - Existing from cache - Do nothing");
                    return new List<(string, string, string, string)>();
                }

                // Set expired time for the processing because, sometimes there is an unknown exception => can't remove processing key
                _memoryCache.Set(processingKey, "1", TimeSpan.FromSeconds(60));

                IEnumerable<string> affectedDeviceIds;
                using (var dbConnection = _dbConnectionFactory.CreateConnection(project.Id))
                {
                    affectedDeviceIds = await dbConnection.QueryAsync<string>(@"select * from fnc_health_check_all_device_signal_quality();",
                    commandTimeout: 300,
                    commandType: CommandType.Text);
                    await dbConnection.CloseAsync();
                }

                if (affectedDeviceIds.Any())
                {
                    _tenantContext.SetTenantId(project.TenantId);
                    _tenantContext.SetSubscriptionId(project.SubscriptionId);
                    _tenantContext.SetProjectId(project.Id);
                    // device state is offline, need to remove all the cache
                    var tasks = new List<Task>();
                    tasks.AddRange(affectedDeviceIds.Select(deviceId =>
                    {
                        var key = CacheKey.DEVICE_STATUS_ONLINE_KEY.GetCacheKey(project.Id, deviceId);
                        return _cache.DeleteAsync(key);
                    }));
                    tasks.AddRange(affectedDeviceIds.Select(id =>
                    {
                        return _domainEventDispatcher.SendAsync(new DeviceConnectionChangedEvent(id, DeviceConnectionStatusConstant.DISCONNECT, _tenantContext));
                    }));

                    _logger.LogTrace($"[ProcessSignalQualityAsync][{project.Id}] - Affected device Ids: {affectedDeviceIds.ToJson()}...");
                    await Task.WhenAll(tasks);
                }

                _memoryCache.Remove(processingKey);

                _logger.LogDebug($"[ProcessSignalQualityAsync][{project.Id}] - Completed!");
                return affectedDeviceIds.Select(deviceId => (deviceId, project.TenantId, project.SubscriptionId, project.Id));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ProcessSignalQualityAsync][{project.Id}] - Failed!");
                return new List<(string, string, string, string)>();
            }
        }

        public async Task TrackingHeartbeatAsync(string projectId, string deviceId)
        {
            var key = CacheKey.DEVICE_STATUS_ONLINE_KEY.GetCacheKey(projectId, deviceId);
            var cacheHit = await _cache.GetStringAsync(key);
            if (cacheHit == "1")
            {
                _logger.LogTrace($"[{projectId}] Device {deviceId} - TrackingHeartbeatAsync: Online / No need to update status");
                // device is online, no need to update the status again
                return;
            }

            using (var dbConnection = _dbConnectionFactory.CreateConnection(projectId))
            {
                var affectedDeviceIds = await dbConnection.QueryAsync<string>(
                      @"select * from fnc_udp_update_device_signal_quality(@DeviceId)",
                      new
                      {
                          DeviceId = deviceId
                      },
                      commandTimeout: 300,
                      commandType: System.Data.CommandType.Text);
                await dbConnection.CloseAsync();

                if (affectedDeviceIds.Any())
                {
                    _logger.LogTrace($"[{projectId}] Device {deviceId} - TrackingHeartbeatAsync: Updated status for device(s): {affectedDeviceIds.ToJson()}");
                    await _cache.StoreStringAsync(key, "1", TimeSpan.FromSeconds(int.MaxValue));

                    var tasks = new List<Task>();
                    tasks.AddRange(affectedDeviceIds.Select(id =>
                    {
                        return _domainEventDispatcher.SendAsync(new DeviceConnectionChangedEvent(id, DeviceConnectionStatusConstant.ACTIVE, _tenantContext));
                    }));

                    await Task.WhenAll(tasks);
                }
            }
            _logger.LogInformation($"[{projectId}] Device {deviceId} - TrackingHeartbeatAsync: Completed!");
        }
    }
}