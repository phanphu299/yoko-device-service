using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AHI.Infrastructure.SharedKernel.Abstraction;
using Device.Application.Repository;
using Device.Domain.Entity;

namespace Device.Persistence.Repository
{
    public class AssetAliasSnapshotRepository : IAssetAliasSnapshotRepository
    {
        private readonly IAssetSnapshotRepository _assetSnapshotRepository;
        private readonly IAssetRepository _assetRepository;
        private readonly ILoggerAdapter<AssetAliasSnapshotRepository> _logger;

        public AssetAliasSnapshotRepository(IAssetRepository assetRepository, IAssetSnapshotRepository assetSnapshotRepository, ILoggerAdapter<AssetAliasSnapshotRepository> logger)
        {
            _assetRepository = assetRepository;
            _assetSnapshotRepository = assetSnapshotRepository;
            _logger = logger;
        }

        public virtual async Task<IEnumerable<TimeSeries>> QueryDataAsync(string timezoneOffset, IEnumerable<HistoricalEntity> metrics, int timeout)
        {
            var start = DateTime.UtcNow;
            var targetAttributeTasks = metrics.Select(x => _assetRepository.FindTargetAttributeAsync(x.AttributeId));
            var targetAttributes = await Task.WhenAll(targetAttributeTasks);

            //task run TimeSeries
            var aliasMapped = targetAttributes.Where(x => x != null);
            var newMetrics = aliasMapped.Select(x => new HistoricalEntity()
            {
                AssetId = x.AssetId,
                AttributeId = x.TargetAttributeId,
                AttributeType = x.AttributeType,
                DataType = x.DataType,
                DeviceId = x.DeviceId,
                MetricKey = x.MetricKey
            });

            var snapshots = await _assetSnapshotRepository.QueryDataAsync(timezoneOffset, newMetrics, timeout);
            // decorate the result
            var rs = aliasMapped.Select(result =>
            {
                var metric = metrics.First(metricInput => metricInput.AttributeId == result.AttributeId);
                var sn = snapshots.FirstOrDefault(snap => snap.AttributeId == result.TargetAttributeId);
                if (sn == null) // With Alias Attribute is Dynamic & Created from Template, we wont have snapshot data.
                    return null;
                return new TimeSeries()
                {
                    AssetId = metric.AssetId,
                    AttributeId = metric.AttributeId,
                    UnixTimestamp = sn.UnixTimestamp,
                    Value = sn.Value,
                    ValueText = sn.ValueText,
                    DataType = metric.DataType,
                    AliasAttributeType = result.AttributeType
                };
            });

            _logger.LogTrace($"Query take {DateTime.UtcNow.Subtract(start).TotalMilliseconds}");
            return rs.Where(r => r != null);
        }
    }
}
