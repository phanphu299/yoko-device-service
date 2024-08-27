
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AHI.Infrastructure.MultiTenancy.Abstraction;
using AHI.Infrastructure.MultiTenancy.Extension;
using Confluent.Kafka;
using Device.Consumer.KraftShared.Constant;
using Device.Consumer.KraftShared.Constants;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Service.Abstraction;
using Microsoft.Extensions.Logging;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace Device.Heartbeat.Handler.Processor
{
    public sealed class TrackingHeartBeatProcessor
    {
        private readonly ILogger<TrackingHeartBeatProcessor> _logger;
        private readonly ITenantContext _tenantContext;
        private readonly IDeviceHeartbeatService _deviceHeartbeatService;
        private readonly IRedisDatabase _cache;

        public TrackingHeartBeatProcessor(
            ITenantContext tenantContext,
            IDeviceHeartbeatService deviceHeartbeatService,
            IRedisDatabase cache,
            ILogger<TrackingHeartBeatProcessor> logger
        )
        {
            _tenantContext = tenantContext;
            _deviceHeartbeatService = deviceHeartbeatService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache;
        }

        public async Task ProcessAsync(string tenantId, string subscriptionId, string projectId,
        IEnumerable<string> deviceIds, CancellationToken cancellationToken)
        {
            // _logger.LogTrace("Processing TrackingHeartBeatProcessor", consumeResult.TopicPartitionOffset, consumeResult.Message.Value);
            var watch = Stopwatch.StartNew();
            
            if (deviceIds != null && deviceIds.Any())
            {
                var hashKey = string.Format(IngestionRedisCacheKeys.DeviceInfoPattern, projectId);
                var tasks = new List<Task>();
                foreach (var deviceId in deviceIds)
                {
                    var deviceInformation = await _cache.HashGetAsync<DeviceInformation>(hashKey, deviceId);
                    if (deviceInformation.EnableHealthCheck)
                    {
                        _logger.LogInformation($"TrackingHeartBeatProcessing - projectId={projectId}, deviceId={deviceId}");

                        tasks.Add(_deviceHeartbeatService.TrackingHeartbeatAsync(projectId, deviceId));
                    }
                }
                if (tasks.Count != 0)
                {
                    await Task.WhenAll(tasks);
                }
            }
            watch.Stop();
            _logger.LogInformation("Tracking Hearbeat completed, took: {ms} ms", watch.ElapsedMilliseconds);
        }
    }
}
