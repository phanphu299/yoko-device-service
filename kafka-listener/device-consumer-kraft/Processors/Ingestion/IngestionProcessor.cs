using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Device.Consumer.KraftShared.Helpers;
using Device.Consumer.KraftShared.Model;
using Device.Consumer.KraftShared.Models.MetricModel;
using Device.Consumer.KraftShared.Models.Options;
using Device.Consumer.KraftShared.Repositories.Abstraction.ReadOnly;
using Device.Consumer.KraftShared.Service.Abstraction;
using Device.Consumer.KraftShared.Service.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Device.Consumer.Kraft.Processor
{

    public sealed class IngestionProcessor
    {
        private JsonSerializerOptions _jsonSerializerOptions = new()
        {
            Converters = { new EmptyStringConverter() },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
        private readonly ILogger<IngestionProcessor> _logger;
        private readonly BatchProcessingOptions _batchOptions;
        private readonly IIngestionProcessEventService _ingestionProcessEventService;
        private readonly IReadOnlyDeviceRepository _readOnlyDeviceRepository;


        public IngestionProcessor(
            ILogger<IngestionProcessor> logger,
            IOptions<BatchProcessingOptions> batchOptions,
            IIngestionProcessEventService ingestionProcessEventService,
            IReadOnlyDeviceRepository readOnlyDeviceRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _batchOptions = batchOptions.Value;
            _ingestionProcessEventService = ingestionProcessEventService;
            _readOnlyDeviceRepository = readOnlyDeviceRepository;
        }

        public async Task ProcessAsync(ConsumeResult<Null, byte[]>[] consumeResult, TopicPartitionOffset topicPartitionOffset, CancellationToken cancellationToken = default)
        {
            try
            {
                var ingestionMessages = consumeResult?.Where(e => e != null && e.Message != null)
                    .Select(e => JsonSerializer.Deserialize<IngestionMessage?>(e.Message.Value, _jsonSerializerOptions))?
                    .Where(i => i != null)?.ToArray();
                //https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1860
                // avoid use any
                if (ingestionMessages == null || ingestionMessages.Length == 0)
                    return;

                var ignoreNullMessages = ingestionMessages.Where(i => i != null && i.RawData != null);

                //Make sub Batch message by Grouping by tenantId, subscriptionId, projectId 

                var groups = ignoreNullMessages.GroupBy(x =>
                new
                {
                    x.TenantId,
                    x.SubscriptionId,
                    x.ProjectId
                }).Select(g => new
                {
                    g.Key.TenantId,
                    g.Key.ProjectId,
                    g.Key.SubscriptionId,
                    Messages = g.ToArray()
                });

                //release memory
                consumeResult = null;
                ingestionMessages = null;
                ignoreNullMessages = null;

                //each group, create new tasks and batch processing message acordingly
                var tasks = groups.Select(group => AssignTenantInfoThenProcessAsync(group.TenantId, group.SubscriptionId, group.ProjectId, group.Messages, topicPartitionOffset));
                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                _logger.LogError("Ingestion_Error Process IngestionProcessor failure {ex}", e);
            }
        }


        /// <summary>
        /// AssignTenantInfoThenProcessAsync set context base on received data, then process by batch
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="projectId"></param>
        /// <param name="rawData"></param>
        /// <returns></returns>
        private async Task AssignTenantInfoThenProcessAsync(string tenantId, string subscriptionId, string projectId, IngestionMessage[] messages, TopicPartitionOffset topicPartitionOffset)
        {
            if (string.IsNullOrWhiteSpace(projectId))
            {
                _logger.LogInformation($"Process IngestionProcessor - Input data ingestion invalid. projectid is null. tenantId={tenantId}, subscriptionId={subscriptionId}, projectId={projectId}");
                return;
            }
            try
            {
                _logger.LogInformation($"AssignTenantInfoThenProcessAsync - start tenantId={tenantId}, subscriptionId={subscriptionId}, projectId={projectId}");
                var watch = Stopwatch.StartNew();
                var deviceInfos = await GetDeviceInfosAsync(projectId);
                //always get triggers from redis
                await _ingestionProcessEventService.ProcessEventAsync(messages, deviceInfos);
                watch.Stop();
                _logger.LogInformation($"=== AssignTenantInfoThenProcessAsync Preprocess done  projectId={projectId} - size = {messages.Length} tooks: {watch.ElapsedMilliseconds} ms ===");
                GC.Collect();
            }
            catch (Exception ex)
            {
                _logger.LogError("Ingestion_Error {data} - {ex}", $"AssignTenantInfoThenProcessAsync - error. tenantId={tenantId}, subscriptionId={subscriptionId}, projectId={projectId}", ex);
            }
        }
        private async Task<IEnumerable<DeviceInformation>> GetDeviceInfosAsync(string projectId)
        {
            if (!_batchOptions.PreloadData.Enabled)
                return [];
            var deviceInfos = await _readOnlyDeviceRepository.GetProjectDevicesAsync(projectId);
            return deviceInfos;
        }

    }
}
