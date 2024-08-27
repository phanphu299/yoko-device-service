using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AHI.Infrastructure.Cache.Abstraction;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Dapper;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Repositories.Abstraction;
using Device.Consumer.KraftShared.Service.Abstraction;
using Microsoft.Extensions.Caching.Memory;

namespace Device.Consumer.KraftShared.Service
{
    public class DeviceHeartbeatService : IDeviceHeartbeatService
    {
        private readonly IDbConnectionFactory _dbConnectionFactory;

        //private readonly ITenantContext _tenantContext;
        private readonly ILoggerAdapter<DeviceHeartbeatService> _logger;
        private readonly ICache _cache;
        private readonly IMemoryCache _memoryCache;
        // private readonly IDomainEventDispatcher _domainEventDispatcher;
        private readonly ITenantContext _tentantContext;

        public DeviceHeartbeatService(IDbConnectionFactory dbConnectionFactory,
            ILoggerAdapter<DeviceHeartbeatService> logger,
            ICache cache,
            // IDomainEventDispatcher domainEventDispatcher,
            ITenantContext tenantContext,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _cache = cache;
            _memoryCache = memoryCache;
            _dbConnectionFactory = dbConnectionFactory;
            //_domainEventDispatcher = domainEventDispatcher;
            _tentantContext = tenantContext;
        }

        public async Task<IEnumerable<(string DeviceId, string TenantId, string SubscriptionId, string ProjectId)>> ProcessSignalQualityAsync(ProjectDto project)
        {
            var processingKey = $"ProcessSignalQualityAsync_{project.Id}";
            var exists = _memoryCache.Get(processingKey);
            if (exists != null)
                return new List<(string, string, string, string)>();

            // Set expired time for the processing because, sometimes there is an unknown exception => can't remove processing key
            _memoryCache.Set(processingKey, "1", TimeSpan.FromSeconds(60));

            IEnumerable<string> affectedDeviceIds = Array.Empty<string>();
            using (var dbConnection = _dbConnectionFactory.CreateConnection(project.Id))
            {
                affectedDeviceIds = await dbConnection.QueryAsync<string>(@"select * from fnc_health_check_all_device_signal_quality();",
                   commandTimeout: 300,
                   commandType: CommandType.Text);
                dbConnection.Close();
            }
            if (affectedDeviceIds.Any())
            {
                _tentantContext.SetTenantId(project.TenantId);
                _tentantContext.SetSubscriptionId(project.SubscriptionId);
                _tentantContext.SetProjectId(project.Id);
                // device state is offline, need to remove all the cache
                var tasks = new List<Task>();
                tasks.AddRange(affectedDeviceIds.Select(deviceId =>
                {
                    var key = $"{project.Id}_device_{deviceId}_status_online";
                    return _cache.DeleteAsync(key);
                }));
                // tasks.AddRange(affectedDeviceIds.Select(id =>
                // {
                //     return _domainEventDispatcher.SendAsync(new DeviceConnectionChangedEvent(id, DeviceConnectionStatusConstant.DISCONNECT, _tentantContext));
                // }));
                // await Task.WhenAll(tasks);
            }
            _memoryCache.Remove(processingKey);
            return affectedDeviceIds.Select(deviceId => (deviceId, project.TenantId, project.SubscriptionId, project.Id));
        }

        public async Task TrackingHeartbeatAsync(string projectId, string deviceId)
        {
            var key = $"{projectId}_device_{deviceId}_status_online";
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
                dbConnection.Close();
                if (affectedDeviceIds.Any())
                {
                    //  _logger.LogTrace($"[{projectId}] Device {deviceId} - TrackingHeartbeatAsync: Updated status for device(s): {affectedDeviceIds.ToJson()}");
                    await _cache.StoreStringAsync(key, "1", TimeSpan.FromSeconds(int.MaxValue));
                    var tasks = new List<Task>();
                    // tasks.AddRange(affectedDeviceIds.Select(id =>
                    // {
                    //     return _domainEventDispatcher.SendAsync(new DeviceConnectionChangedEvent(id, DeviceConnectionStatusConstant.ACTIVE, _tentantContext));
                    // }));
                    // await Task.WhenAll(tasks);
                }
            }
            _logger.LogInformation($"[{projectId}] Device {deviceId} - TrackingHeartbeatAsync: Completed!");
        }
    }
}
